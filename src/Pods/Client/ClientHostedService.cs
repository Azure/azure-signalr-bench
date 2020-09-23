// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

using Azure.SignalRBench.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Azure.SignalRBench.Client
{
    public class ClientHostedService : IHostedService
    {
        private readonly IConfiguration _configuration;
        private readonly MessageClientHolder _messageClientHolder;

        public ClientHostedService(IConfiguration configuration, MessageClientHolder messageClientHolder)
        {
            _configuration = configuration;
            _messageClientHolder = messageClientHolder;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _messageClientHolder.AddMessageHandlers(
                _configuration[Constants.ConfigurationKeys.RedisConnectionStringKey],
                _configuration[Constants.ConfigurationKeys.PodNameStringKey]);
        }

        public Task StopAsync(CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
