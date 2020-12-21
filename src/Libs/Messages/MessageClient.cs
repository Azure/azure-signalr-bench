// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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

        public async Task WithHandlers(params MessageHandler[] handlers)
        {
            foreach (var handler in handlers ?? throw new ArgumentNullException(nameof(handlers)))
            {
                var cmq = await _subscriber.SubscribeAsync(
                    $"{TestId}:{handler.Role ?? _sender}:{handler.Command}:{handler.Type}");
                cmq.OnMessage(cm => handler.Handle(cm.Message));
            }
        }

        public async Task SendCommandAsync(string target, CommandMessage commandMessage)
        {
            var ackId = Interlocked.Increment(ref _ackId);
            commandMessage.Sender = _sender;
            commandMessage.AckId = ackId;
            await _subscriber.PublishAsync($"{TestId}:{target}:{commandMessage.Command}:{nameof(MessageType.Command)}",
                JsonConvert.SerializeObject(commandMessage));
        }

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
            _connection.Dispose();
        }
    }
}