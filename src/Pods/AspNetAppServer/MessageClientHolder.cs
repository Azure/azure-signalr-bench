// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Messages;
using Microsoft.Extensions.Logging;

namespace AspNetAppServer
{
    public class MessageClientHolder
    {
        private MessageClient _messageClient;
        public async Task InitializeAsync(string testId, string connectionString, string podName)
        {
            if (_messageClient != null)
            {
                throw new InvalidOperationException();
            }
            _messageClient = await MessageClient.ConnectAsync(connectionString, testId, podName);
            await _messageClient.WithHandlers(
                MessageHandler.CreateCommandHandler(Commands.General.Crash, Crash),
                MessageHandler.CreateCommandHandler(Roles.AppServers, Commands.General.Crash, Crash));
            Console.WriteLine("Message handlers inited.");
            await _messageClient.ReportReadyAsync(new ReportReadyParameters() { Role = Roles.AppServers });
            Console.WriteLine("Server ready acked.");
        }

        private Task Crash(CommandMessage command)
        {
            Console.WriteLine("AppServer start to crash");
            Environment.Exit(1);
            return Task.CompletedTask;
        }
    }
}
