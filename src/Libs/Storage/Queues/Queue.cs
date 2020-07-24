// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

using Azure.Storage.Queues;
using Newtonsoft.Json;

namespace Azure.SignalRBench.Storage
{
    public class Queue<T> : IQueue<T>
    {
        private readonly QueueClient _client;

        internal Queue(string connectionString, string queueName)
        {
            _client = new QueueClient(connectionString, queueName);
        }

        public Task CreateIfNotExistedAsync() =>
            _client.CreateIfNotExistsAsync();

        public async Task<QueueMessage<T>> ReceiveAsync(
            TimeSpan? visibilityTimeout,
            CancellationToken cancellationToken)
        {
            while (true)
            {
                var messages = await _client.ReceiveMessagesAsync(1, visibilityTimeout ?? TimeSpan.FromMinutes(5), cancellationToken);

                if (messages.Value.Length == 0)
                {
                    return null;
                }
                var msg = messages.Value[0];
                if (msg.DequeueCount > 3)
                {
                    await _client.DeleteMessageAsync(msg.MessageId, msg.PopReceipt, cancellationToken);
                    continue;
                }
                return new QueueMessage<T>(msg.MessageId, msg.PopReceipt, JsonConvert.DeserializeObject<T>(msg.MessageText));
            }
        }

        public Task SendAsync(T message, CancellationToken cancellationToken) =>
            _client.SendMessageAsync(JsonConvert.SerializeObject(message), cancellationToken);

        public async Task UpdateAsync(QueueMessage<T> message, TimeSpan visibilityTimeout, CancellationToken cancellationToken)
        {
            var response = await _client.UpdateMessageAsync(message.MessageId, message.PopReceipt, JsonConvert.SerializeObject(message.Value), visibilityTimeout, cancellationToken);
            message.PopReceipt = response.Value.PopReceipt;
        }

        public Task DeleteAsync(QueueMessage<T> message, CancellationToken cancellationToken) =>
            _client.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);
    }
}
