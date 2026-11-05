// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Azure.SignalRBench.Messages
{
    public class MessageClient : IMessageClient, IDisposable
    {
        private readonly IConnectionMultiplexer _connection;
        private readonly ISubscriber _subscriber;
        public string TestId { get; set; }
        private readonly string _sender;
        private readonly IDatabase _database;
        private int _ackId;
        private readonly object _subscriptionsLock = new object();
        private readonly HashSet<HandlerScope> _subscriptions = new HashSet<HandlerScope>();
        private bool _disposed;

        private MessageClient(IConnectionMultiplexer connection, ISubscriber subscriber, IDatabase database,
            string testId, string sender)
        {
            _connection = connection;
            _subscriber = subscriber;
            _database = database;
            TestId = testId;
            _sender = sender;
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

            var connection =
                await ConnectionMultiplexer.ConnectAsync(connectionString ??
                                                         throw new ArgumentNullException(nameof(connectionString)));
            var subscriber = connection.GetSubscriber();
            var database = connection.GetDatabase();
            var result = new MessageClient(connection, subscriber, database, testId, sender);
            return result;
        }

        public async Task<IDisposable> WithHandlers(params MessageHandler[] handlers)
        {
            var scope = new HandlerScope(this);
            try
            {
                foreach (var handler in handlers ?? throw new ArgumentNullException(nameof(handlers)))
                {
                    var cmq = await _subscriber.SubscribeAsync(
                        $"{TestId}:{handler.Role ?? _sender}:{handler.Command}:{handler.Type}");
                    scope.Add(cmq);
                    cmq.OnMessage((Func<ChannelMessage, Task>)(cm =>
                        scope.IsDisposed ? Task.CompletedTask : handler.Handle(cm.Message)));
                }
                lock (_subscriptionsLock)
                {
                    if (_disposed)
                        throw new ObjectDisposedException(nameof(MessageClient));
                    _subscriptions.Add(scope);
                }
                return scope;
            }
            catch
            {
                scope.Dispose();
                throw;
            }
        }

        public CommandMessage PrepareCommand(CommandMessage commandMessage)
        {
            if (commandMessage == null)
                throw new ArgumentNullException(nameof(commandMessage));
            commandMessage.Sender = _sender;
            commandMessage.AckId = Interlocked.Increment(ref _ackId);
            return commandMessage;
        }

        public async Task PublishPreparedCommandAsync(string target, CommandMessage commandMessage)
        {
            await _subscriber.PublishAsync($"{TestId}:{target}:{commandMessage.Command}:{nameof(MessageType.Command)}",
                JsonConvert.SerializeObject(commandMessage));
        }

        public Task SendCommandAsync(string target, CommandMessage commandMessage) =>
            PublishPreparedCommandAsync(target, PrepareCommand(commandMessage));

        public async Task AckAsync(CommandMessage commandMessage, AckStatus status, string? error = null,
            double? progress = null)
        {
            var message = new AckMessage
                {Sender = _sender, AckId = commandMessage.AckId, Status = status, Error = error, Progress = progress};
            await _subscriber.PublishAsync(
                $"{TestId}:{commandMessage.Sender}:{commandMessage.Command}:{nameof(MessageType.Ack)}",
                JsonConvert.SerializeObject(message));
        }

        public void Dispose()
        {
            HandlerScope[] scopes;
            lock (_subscriptionsLock)
            {
                if (_disposed)
                    return;
                _disposed = true;
                scopes = _subscriptions.ToArray();
            }
            try
            {
                Exception? failure = null;
                foreach (var scope in scopes)
                {
                    try { scope.Dispose(); }
                    catch (Exception e) { failure ??= e; }
                }
                if (failure != null)
                    throw failure;
            }
            finally
            {
                _connection.Dispose();
            }
        }

        private sealed class HandlerScope : IDisposable
        {
            private readonly MessageClient _owner;
            private readonly List<ChannelMessageQueue> _queues = new List<ChannelMessageQueue>();
            private int _disposed;

            public HandlerScope(MessageClient owner) => _owner = owner;
            public bool IsDisposed => Volatile.Read(ref _disposed) != 0;
            public void Add(ChannelMessageQueue queue) => _queues.Add(queue);

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0)
                    return;
                try
                {
                    Exception? failure = null;
                    foreach (var queue in _queues)
                    {
                        try { queue.Unsubscribe(); }
                        catch (Exception e) { failure ??= e; }
                    }
                    if (failure != null)
                        throw failure;
                }
                finally
                {
                    _queues.Clear();
                    lock (_owner._subscriptionsLock)
                        _owner._subscriptions.Remove(this);
                }
            }
        }
    }
}