// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using Microsoft.Azure.SignalRBench.Messages;
using Xunit;

namespace Microsoft.Azure.SignalRBench.Tests.MessagesTest
{
    public class MessageClientTest
    {
        [SkippableFact]
        public async Task TestMessageClient()
        {
            var redis = Requirements.RequireRedis();
            const string sender = nameof(TestMessageClient);
            const string expectedCommand = "Test";
            var commandTcs = new TaskCompletionSource<CommandMessage>();
            var ackTcs = new TaskCompletionSource<AckMessage>();
            using var client = await MessageClient.ConnectAsync(
                redis,
                sender,
                MessageHandler.CreateCommandHandler(
                    sender,
                    cmd =>
                    {
                        commandTcs.TrySetResult(cmd);
                        return Task.CompletedTask;
                    }),
                MessageHandler.CreateAckHandler(
                    sender,
                    ack =>
                    {
                        ackTcs.TrySetResult(ack);
                        return Task.CompletedTask;
                    }));

            await client.SendCommandAsync(sender, expectedCommand);
            var cmd = await commandTcs.Task.WithTimeout();
            Assert.Equal(expectedCommand, cmd.Command);
            Assert.Equal(sender, cmd.Sender);
            Assert.True(cmd.AckId > 0);
            Assert.Null(cmd.Parameters);

            await client.AckCompletedAsync(cmd);
            var ack = await ackTcs.Task.WithTimeout();
            Assert.Equal(sender, ack.Sender);
            Assert.Equal(cmd.AckId, ack.AckId);
            Assert.True(ack.IsCompleted);
            Assert.Null(ack.Progress);
        }
    }
}
