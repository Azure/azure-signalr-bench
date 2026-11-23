// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Azure.SignalRBench.Messages
{
    public class MessageClient : IMessageClient, IDisposable
    {
        private static readonly TimeSpan DefaultAckTimeout = TimeSpan.FromMinutes(60);

        private readonly IConnectionMultiplexer _connection;
        private readonly ISubscriber _subscriber;
        public string TestId { get; set; }
        private readonly string _sender;
        private readonly IDatabase _database;
        private readonly AckHandler _ackHandler;
        private int _ackId;

        private MessageClient(IConnectionMultiplexer connection, string testId, string sender)
        {
            _connection = connection;
            _subscriber = connection.GetSubscriber();
            _database = connection.GetDatabase();
            TestId = testId;
            _sender = sender;
            _ackHandler = new AckHandler(this);
        }

        public async Task<string> GetAsync(string key)
        {
            return await _database.HashGetAsync(TestId, key);
        }

        public async Task SetAsync(string key, string value)
        {
            await _database.HashSetAsync(TestId, key, value);
        }

        public async Task DeleteHashTableAsync()
        {
            await _database.KeyDeleteAsync(TestId);
        }

        public async static Task<MessageClient> ConnectAsync(string connectionString, string testId, string sender)
        {
            if (string.IsNullOrEmpty(testId))
            {
                throw new ArgumentException("Test id cannot be empty.", nameof(testId));
            }

            var connection = await ConnectionMultiplexer.ConnectAsync(connectionString);
            return new MessageClient(connection, testId, sender);
        }

        public async Task<IDisposable> WithHandlers(params MessageHandler[] handlers)
        {
            var scope = new HandlerScope();
            foreach (var handler in handlers)
            {
                var cmq = await _subscriber.SubscribeAsync(
                    $"{TestId}:{handler.Role ?? _sender}:{handler.Command}:{handler.Type}");
                scope.Add(cmq);
                cmq.OnMessage((Func<ChannelMessage, Task>)(cm =>
                    scope.IsDisposed ? Task.CompletedTask : handler.Handle(cm.Message)));
            }
            return scope;
        }

        public Task SendCommandAsync(string target, CommandMessage commandMessage) =>
            PublishPreparedCommandAsync(target, PrepareCommand(commandMessage));

        public Task SendCommandAndWaitForAcksAsync(string target, CommandMessage commandMessage,
            IReadOnlyCollection<string> expectedSenders, CancellationToken cancellationToken, TimeSpan? timeout = null) =>
            _ackHandler.SendAndWaitAsync(target, commandMessage, expectedSenders, timeout ?? DefaultAckTimeout,
                cancellationToken);

        public async Task AckAsync(CommandMessage commandMessage, AckStatus status, string? error = null,
            double? progress = null)
        {
            var message = new AckMessage
                {Sender = _sender, AckId = commandMessage.AckId, Status = status, Error = error, Progress = progress};
            await _subscriber.PublishAsync(
                $"{TestId}:{commandMessage.Sender}:{commandMessage.Command}:{nameof(MessageType.Ack)}",
                JsonConvert.SerializeObject(message));
        }

        internal CommandMessage PrepareCommand(CommandMessage commandMessage)
        {
            commandMessage.Sender = _sender;
            commandMessage.AckId = Interlocked.Increment(ref _ackId);
            return commandMessage;
        }

        internal async Task PublishPreparedCommandAsync(string target, CommandMessage commandMessage)
        {
            await _subscriber.PublishAsync($"{TestId}:{target}:{commandMessage.Command}:{nameof(MessageType.Command)}",
                JsonConvert.SerializeObject(commandMessage));
        }

        // Closing the connection drops every subscription.
        public void Dispose() => _connection.Dispose();

        private sealed class HandlerScope : IDisposable
        {
            private readonly List<ChannelMessageQueue> _queues = new List<ChannelMessageQueue>();
            private volatile bool _disposed;

            public bool IsDisposed => _disposed;

            public void Add(ChannelMessageQueue queue) => _queues.Add(queue);

            public void Dispose()
            {
                _disposed = true;
                foreach (var queue in _queues)
                    queue.Unsubscribe();
            }
        }
    }
}
