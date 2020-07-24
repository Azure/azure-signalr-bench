// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Azure.SignalRBench.Storage;
using Xunit;

namespace Azure.SignalRBench.Tests.StorageTest
{
    public class QueueTest
    {
        [SkippableFact]
        public async Task TestQueueCrud()
        {
            var storage = new PerfStorage(Requirements.RequireStorage());
            var queue = await storage.GetQueueAsync<TestEntity>(nameof(TestQueueCrud).ToLower(), true);
            var span = TimeSpan.FromSeconds(5);
            var entity = new TestEntity();
            entity.Id = entity.GetHashCode();
            await queue.SendAsync(entity);
            var message = await RecieveAsync(queue, span, entity.Id);
            Assert.Null(await queue.ReceiveAsync(span));
            message.Value.Id++;
            await queue.UpdateAsync(message, default);

            var msg = await queue.ReceiveAsync(span);
            Assert.NotNull(msg);
            Assert.Equal(entity.Id + 1, msg.Value.Id);

            await queue.DeleteAsync(msg);
            Assert.Null(await queue.ReceiveAsync(span));
        }

        [SkippableFact]
        public async Task TestQueueLongRun()
        {
            var storage = new PerfStorage(Requirements.RequireStorage());
            var queue = await storage.GetQueueAsync<TestEntity>(nameof(TestQueueLongRun).ToLower(), true);
            var span = TimeSpan.FromSeconds(5);
            var entity = new TestEntity();
            entity.Id = entity.GetHashCode();
            await queue.SendAsync(entity);
            var message = await RecieveAsync(queue, span, entity.Id);

            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                await queue.UpdateAsync(message, span);
            }

            Assert.Null(await queue.ReceiveAsync(span));
            await queue.UpdateAsync(message, default);

            var msg = await queue.ReceiveAsync(span);
            Assert.NotNull(msg);

            await queue.DeleteAsync(msg);
            Assert.Null(await queue.ReceiveAsync(span));
        }

        [SkippableFact]
        public async Task TestQueueSendAndConsume()
        {
            var storage = new PerfStorage(Requirements.RequireStorage());
            var queue = await storage.GetQueueAsync<TestEntity>(nameof(TestQueueSendAndConsume).ToLower(), true);
            var list = new List<TestEntity>();
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var consumerTask = Task.Run(
                async () =>
                {
                    await foreach (var m in queue.Consume(cancellationToken: cts.Token))
                    {
                        list.Add(m.Value);
                        await queue.DeleteAsync(m);
                        if (m.Value.IsCompleted)
                        {
                            return;
                        }
                    }
                },
                cts.Token);
            var expected = new List<TestEntity>();
            for (int i = 0; i < 10; i++)
            {
                expected.Add(new TestEntity { Id = i });
                await queue.SendAsync(expected.Last());
            }
            expected.Add(new TestEntity { IsCompleted = true });
            await queue.SendAsync(expected.Last());

            await consumerTask;
            Assert.Equal(expected, list);
        }

        private static async Task<QueueMessage<TestEntity>> RecieveAsync(IQueue<TestEntity> queue, TimeSpan span, int id)
        {
            var message = await queue.ReceiveAsync(span);
            Assert.NotNull(message);
            while (message.Value.Id != id)
            {
                await queue.DeleteAsync(message);
                message = await queue.ReceiveAsync(span);
                Assert.NotNull(message);
            }

            return message;
        }

        public class TestEntity : IEquatable<TestEntity>
        {
            public int Id { get; set; }
            public bool IsCompleted { get; set; }

            public bool Equals([AllowNull] TestEntity other) =>
                Id == other?.Id && IsCompleted == other?.IsCompleted;

            public override bool Equals(object obj) =>
                Equals(obj as TestEntity);

            public override int GetHashCode() =>
                HashCode.Combine(Id, IsCompleted);
        }
    }
}
