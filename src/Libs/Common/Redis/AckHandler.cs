// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.SignalRBench.Messages
{
    /// <summary>
    /// Publishes commands and waits for their acks. Each command has one standing ack subscription for the
    /// lifetime of the client, and incoming acks are routed to the waiting call by AckId.
    /// </summary>
    /// <remarks>
    /// Never subscribe to an ack channel per call: StackExchange.Redis completes a second subscribe to a channel
    /// before Redis has applied the first SUBSCRIBE, so acks published in that gap are dropped.
    /// </remarks>
    internal sealed class AckHandler
    {
        private readonly MessageClient _client;
        private readonly ConcurrentDictionary<string, Lazy<Task>> _subscriptions =
            new ConcurrentDictionary<string, Lazy<Task>>();
        private readonly ConcurrentDictionary<int, PendingAcks> _pending = new ConcurrentDictionary<int, PendingAcks>();

        public AckHandler(MessageClient client) => _client = client;

        public async Task SendAndWaitAsync(string target, CommandMessage command, IReadOnlyCollection<string> senders,
            TimeSpan timeout, CancellationToken cancellationToken)
        {
            _client.PrepareCommand(command);
            var acks = new PendingAcks(command, senders);
            _pending[command.AckId] = acks;
            try
            {
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                using var stop = deadline.Token.Register(() => acks.Stop(cancellationToken));
                deadline.CancelAfter(timeout);
                var publish = SubscribeThenPublishAsync(target, command);
                // Fail fast on a publish error, timeout, cancellation or faulted ack; succeed once both complete.
                await await Task.WhenAny(publish, acks.Task);
                await Task.WhenAll(publish, acks.Task);
            }
            finally
            {
                _pending.TryRemove(command.AckId, out _);
            }
        }

        private async Task SubscribeThenPublishAsync(string target, CommandMessage command)
        {
            // Every concurrent caller awaits the same server-confirmed subscription before publishing.
            await _subscriptions.GetOrAdd(command.Command, CreateSubscription).Value;
            await _client.PublishPreparedCommandAsync(target, command);
        }

        private Lazy<Task> CreateSubscription(string command) =>
            new Lazy<Task>(() => _client.WithHandlers(MessageHandler.CreateAckHandler(command, OnAck)));

        private Task OnAck(AckMessage ack)
        {
            if (_pending.TryGetValue(ack.AckId, out var acks))
                acks.OnAck(ack);
            return Task.CompletedTask;
        }

        private sealed class PendingAcks
        {
            private readonly CommandMessage _command;
            private readonly HashSet<string> _missing;
            private readonly TaskCompletionSource<bool> _completion =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            public PendingAcks(CommandMessage command, IEnumerable<string> senders)
            {
                _command = command;
                _missing = new HashSet<string>(senders);
                if (_missing.Count == 0)
                    _completion.SetResult(true);
            }

            public Task Task => _completion.Task;

            public void OnAck(AckMessage ack)
            {
                lock (_missing)
                {
                    if (!_missing.Contains(ack.Sender))
                        return;
                    switch (ack.Status)
                    {
                        case AckStatus.Completed:
                            _missing.Remove(ack.Sender);
                            if (_missing.Count == 0)
                                _completion.TrySetResult(true);
                            break;
                        case AckStatus.Faulted:
                            _completion.TrySetException(
                                new RpcFaultedException($"{this}, sender {ack.Sender} failed. {ack.Error}"));
                            break;
                        case AckStatus.Canceled:
                            _completion.TrySetException(
                                new RpcFaultedException($"{this}, sender {ack.Sender} canceled remotely. {ack.Error}"));
                            break;
                    }
                }
            }

            public void Stop(CancellationToken cancellationToken)
            {
                lock (_missing)
                {
                    if (cancellationToken.IsCancellationRequested)
                        _completion.TrySetCanceled(cancellationToken);
                    else
                        _completion.TrySetException(new TimeoutException(
                            $"{this} timed out; missing senders: {string.Join(", ", _missing.OrderBy(sender => sender))}."));
                }
            }

            public override string ToString() => $"Command {_command.Command}, AckId {_command.AckId}";
        }
    }
}
