// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

using Azure.SignalRBench.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Azure.SignalRBench.AppServer
{
    public class ServerHostedService : IHostedService
    {
        private readonly IConfiguration _configuration;
        private readonly MessageClientHolder _messageClientHolder;

        public ServerHostedService(IConfiguration configuration, MessageClientHolder messageClientHolder)
        {
            _configuration = configuration;
            _messageClientHolder = messageClientHolder;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _messageClientHolder.InitializeAsync(
                _configuration[Constants.EnvVariableKey.RedisConnectionStringKey],
                _configuration[Constants.EnvVariableKey.PodNameStringKey]);
        }

        public Task StopAsync(CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
