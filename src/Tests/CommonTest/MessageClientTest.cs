// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Azure.SignalRBench.Messages;
using Xunit;

namespace Azure.SignalRBench.Tests.MessagesTest
{
    public class MessageClientTest
    {
        [SkippableFact]
        public async Task TestMessageClient()
        {
            const string TestId = nameof(TestMessageClient);
            var redis = Requirements.RequireRedis();
            const string sender1 = "1";
            const string sender2 = "2";
            const string expectedCommand = "Test";
            var commandTcs = new TaskCompletionSource<CommandMessage>();
            var ackTcs = new TaskCompletionSource<AckMessage>();
            using var client1 = await MessageClient.ConnectAsync(redis, TestId, sender1);
            await client1.WithHandlers(
                MessageHandler.CreateAckHandler(
                    expectedCommand,
                    ack =>
                    {
                        ackTcs.TrySetResult(ack);
                        return Task.CompletedTask;
                    }));
            using var client2 = await MessageClient.ConnectAsync(redis, TestId, sender2);
            await client2.WithHandlers(
                MessageHandler.CreateCommandHandler(
                    expectedCommand,
                    cmd =>
                    {
                        commandTcs.TrySetResult(cmd);
                        return Task.CompletedTask;
                    }));
            var command = new CommandMessage { Command = expectedCommand };
            await client1.SendCommandAsync(sender2, command);

            var cmd = await commandTcs.Task.OrTimeout();
            Assert.Equal(expectedCommand, cmd.Command);
            Assert.Equal(sender1, cmd.Sender);
            Assert.True(cmd.AckId > 0);
            Assert.Null(cmd.Parameters);

            await client2.AckCompletedAsync(cmd);

            var ack = await ackTcs.Task.OrTimeout();
            Assert.Equal(sender2, ack.Sender);
            Assert.Equal(cmd.AckId, ack.AckId);
            Assert.Equal(AckStatus.Completed, ack.Status);
            Assert.Null(ack.Progress);
        }

        [SkippableFact]
        public async Task ParallelCommandsReceiveEveryAckAcrossRounds()
        {
            var redis = Requirements.RequireRedis();
            var testId = nameof(ParallelCommandsReceiveEveryAckAcrossRounds) + Guid.NewGuid().ToString("N");
            const string command = "Test";
            const int responderCount = 20;
            const int rounds = 50;
            using var coordinator = await MessageClient.ConnectAsync(redis, testId, "coordinator");
            var responders = new List<MessageClient>();
            try
            {
                for (var i = 0; i < responderCount; i++)
                {
                    var responder = await MessageClient.ConnectAsync(redis, testId, $"responder-{i}");
                    responders.Add(responder);
                    await responder.WithHandlers(
                        MessageHandler.CreateCommandHandler(command, cmd => responder.AckCompletedAsync(cmd)));
                }
                var names = Enumerable.Range(0, responderCount).Select(i => $"responder-{i}").ToArray();
                for (var round = 0; round < rounds; round++)
                {
                    // A busy default thread pool delays subscription writes, as on a coordinator between rounds.
                    for (var k = 0; k < Environment.ProcessorCount; k++)
                        ThreadPool.UnsafeQueueUserWorkItem(_ => Thread.Sleep(5), null);
                    await Task.WhenAll(names.Select(name => coordinator.SendCommandAndWaitForAcksAsync(name,
                        new CommandMessage { Command = command }, new[] { name }, CancellationToken.None,
                        TimeSpan.FromSeconds(5))));
                }
            }
            finally
            {
                foreach (var responder in responders)
                    responder.Dispose();
            }
        }

        [SkippableFact]
        public async Task WaitForAcksReportsMissingSendersOnTimeout()
        {
            var redis = Requirements.RequireRedis();
            var testId = nameof(WaitForAcksReportsMissingSendersOnTimeout) + Guid.NewGuid().ToString("N");
            using var coordinator = await MessageClient.ConnectAsync(redis, testId, "coordinator");
            using var responder = await MessageClient.ConnectAsync(redis, testId, "responder");
            await responder.WithHandlers(MessageHandler.CreateCommandHandler("Test", _ => Task.CompletedTask));

            var e = await Assert.ThrowsAsync<TimeoutException>(() => coordinator.SendCommandAndWaitForAcksAsync(
                "responder", new CommandMessage { Command = "Test" }, new[] { "responder" }, CancellationToken.None,
                TimeSpan.FromSeconds(1)));
            Assert.Contains("missing senders: responder.", e.Message);
        }

        [SkippableFact]
        public async Task WaitForAcksFailsOnFaultedAck()
        {
            var redis = Requirements.RequireRedis();
            var testId = nameof(WaitForAcksFailsOnFaultedAck) + Guid.NewGuid().ToString("N");
            using var coordinator = await MessageClient.ConnectAsync(redis, testId, "coordinator");
            using var responder = await MessageClient.ConnectAsync(redis, testId, "responder");
            await responder.WithHandlers(
                MessageHandler.CreateCommandHandler("Test", cmd => responder.AckFaultedAsync(cmd, "boom")));

            var e = await Assert.ThrowsAsync<RpcFaultedException>(() => coordinator.SendCommandAndWaitForAcksAsync(
                "responder", new CommandMessage { Command = "Test" }, new[] { "responder" }, CancellationToken.None,
                TimeSpan.FromSeconds(5)));
            Assert.Contains("sender responder failed. boom", e.Message);
        }

        [SkippableFact]
        public async Task WaitForAcksStopsOnCancellation()
        {
            var redis = Requirements.RequireRedis();
            var testId = nameof(WaitForAcksStopsOnCancellation) + Guid.NewGuid().ToString("N");
            using var coordinator = await MessageClient.ConnectAsync(redis, testId, "coordinator");
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => coordinator.SendCommandAndWaitForAcksAsync(
                "nobody", new CommandMessage { Command = "Test" }, new[] { "nobody" }, cts.Token,
                TimeSpan.FromSeconds(30)));
        }
    }
}
