﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

using Azure.SignalRBench.Common;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Azure.SignalRBench.Client
{
    public class ClientHostedService : IHostedService
    {
        private readonly IConfiguration _configuration;
        private readonly MessageClientHolder _messageClientHolder;
        private readonly AzureEventSourceLogForwarder _forwarder;

        public ClientHostedService(IConfiguration configuration, MessageClientHolder messageClientHolder, ILoggerFactory loggerFactory)
        {
            _configuration = configuration;
            _messageClientHolder = messageClientHolder;
            _forwarder = new AzureEventSourceLogForwarder(loggerFactory);
            _forwarder.Start();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _messageClientHolder.InitializeAsync(
                _configuration[PerfConstants.ConfigurationKeys.TestIdKey],
                _configuration[PerfConstants.ConfigurationKeys.RedisConnectionStringKey],
                _configuration[PerfConstants.ConfigurationKeys.PodNameStringKey]);
        }

        public Task StopAsync(CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
