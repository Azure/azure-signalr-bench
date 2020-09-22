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
        private readonly string _sender;
        private int _ackId;

        private MessageClient(IConnectionMultiplexer connection, ISubscriber subscriber, string sender)
        {
            _connection = connection;
            _subscriber = subscriber;
            _sender = sender;
        }

        public async static Task<MessageClient> ConnectAsync(string connectionString, string sender)
        {
            var connection = await ConnectionMultiplexer.ConnectAsync(connectionString ?? throw new ArgumentNullException(nameof(connectionString)));
            var subscriber = connection.GetSubscriber();
            var result = new MessageClient(connection, subscriber, sender);
            return result;
        }

        public async Task WithHandlers(params MessageHandler[] handlers)
        {
            foreach (var handler in handlers ?? throw new ArgumentNullException(nameof(handlers)))
            {
                var cmq = await _subscriber.SubscribeAsync($"{handler.Role ?? _sender}:{handler.Command}:{handler.Type}");
                cmq.OnMessage(cm => handler.Handle(cm.Message));
            }
        }

        public async Task SendCommandAsync(string target, CommandMessage commandMessage)
        {
            var ackId = Interlocked.Increment(ref _ackId);
            commandMessage.Sender = _sender;
            commandMessage.AckId = ackId;
            await _subscriber.PublishAsync($"{target}:{commandMessage.Command}:{nameof(MessageType.Command)}", JsonConvert.SerializeObject(commandMessage));
        }

        public async Task AckAsync(CommandMessage commandMessage, bool isCompleted, double? progress = null)
        {
            var message = new AckMessage { Sender = _sender, AckId = commandMessage.AckId, IsCompleted = isCompleted, Progress = progress };
            await _subscriber.PublishAsync($"{commandMessage.Sender}:{commandMessage.Command}:{nameof(MessageType.Ack)}", JsonConvert.SerializeObject(message));
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
