// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

using Azure.SignalRBench.Common;
using Azure.SignalRBench.Messages;
using Microsoft.Extensions.Logging;

namespace Azure.SignalRBench.AppServer
{
    public class MessageClientHolder
    {
        private readonly ILogger<MessageClientHolder> _logger;
        private MessageClient? _messageClient;

        public MessageClientHolder(ILogger<MessageClientHolder> logger)
        {
            _logger = logger;
        }

        public MessageClient MessageClient =>
            _messageClient ?? throw new InvalidOperationException();

        public async Task InitializeAsync(string connectionString, string podName)
        {
            if (_messageClient != null)
            {
                throw new InvalidOperationException();
            }
            _messageClient = await MessageClient.ConnectAsync(connectionString, podName);
            await _messageClient.WithHandlers(
                MessageHandler.CreateCommandHandler(Commands.General.Crash, Crash),
                MessageHandler.CreateCommandHandler(Roles.AppServers, Commands.General.Crash, Crash));
        }

        private Task Crash(CommandMessage command)
        {
            _logger.LogWarning("AppServer start to crash");
            Environment.Exit(1);
            return Task.CompletedTask;
        }
    }
}
