﻿using Azure.SignalRBench.Common;
using Azure.SignalRBench.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppServer
{
    public class MessageClientHolder
    {
        private MessageClient messageClient;
        private const string RedisConnectionStringKey = "Redis:SignalR:ConnectionString";

        public MessageClientHolder(IConfiguration configuration, ILogger<MessageClientHolder> logger)
        {
            const string sender = "AppServer";
            var crash = MessageHandler.CreateCommandHandler(Commands.General.Crash, cmd =>
            {
                logger.LogWarning("AppServer start to crash");
                Environment.Exit(1);
                return Task.CompletedTask;
            });
            Task.Run(async () => messageClient = await MessageClient.ConnectAsync(configuration[RedisConnectionStringKey], sender, crash));
        }
    }
}
