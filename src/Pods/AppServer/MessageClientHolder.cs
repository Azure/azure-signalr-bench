// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

using Azure.SignalRBench.Common;
using Azure.SignalRBench.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Azure.SignalRBench.AppServer
{
    public class MessageClientHolder
    {
        private MessageClient? _messageClient;

        public MessageClientHolder(IConfiguration configuration, ILogger<MessageClientHolder> logger)
        {
            const string sender = "AppServer";
            var crash = MessageHandler.CreateCommandHandler(Commands.General.Crash, cmd =>
            {
                logger.LogWarning("AppServer start to crash");
                Environment.Exit(1);
                return Task.CompletedTask;
            });
            Task.Run(async () => _messageClient = await MessageClient.ConnectAsync(configuration[Constants.EnvVariableKey.RedisConnectionStringKey], sender, crash));
        }

        public MessageClient MessageClient =>
            _messageClient ?? throw new InvalidOperationException();
    }
}
