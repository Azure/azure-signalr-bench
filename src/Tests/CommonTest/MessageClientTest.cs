// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    }
}
