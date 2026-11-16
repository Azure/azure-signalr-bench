// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Azure.SignalRBench.AppServer.Readiness;
using Azure.SignalRBench.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Azure.SignalRBench.AppServer
{
    /// <summary>
    /// Connects the server to the coordinator and reports Ready only once the server can serve clients.
    /// </summary>
    public class ServerHostedService : IHostedLifecycleService
    {
        private static readonly TimeSpan ReadyTimeout = TimeSpan.FromMinutes(2);

        private readonly IConfiguration _configuration;
        private readonly MessageClientHolder _messageClientHolder;
        private readonly IEnumerable<IReadinessCheck> _readinessChecks;
        private readonly ILogger<ServerHostedService> _logger;

        public ServerHostedService(IConfiguration configuration, MessageClientHolder messageClientHolder,
            IEnumerable<IReadinessCheck> readinessChecks, ILogger<ServerHostedService> logger)
        {
            _configuration = configuration;
            _messageClientHolder = messageClientHolder;
            _readinessChecks = readinessChecks;
            _logger = logger;
        }

        // Connect early so that coordinator commands (e.g. Crash) are handled during startup.
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _messageClientHolder.InitializeAsync(
                _configuration[PerfConstants.ConfigurationKeys.TestIdKey],
                _configuration[PerfConstants.ConfigurationKeys.RedisConnectionStringKey],
                _configuration[PerfConstants.ConfigurationKeys.PodNameStringKey]);
        }

        // StartedAsync runs after every hosted service has started, including the web server, so the server is
        // already listening here. A failed or timed-out check fails host startup instead of reporting Ready.
        public async Task StartedAsync(CancellationToken cancellationToken)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(ReadyTimeout);
            try
            {
                foreach (var check in _readinessChecks)
                {
                    await check.WaitUntilReadyAsync(timeout.Token);
                    _logger.LogInformation("Readiness check '{Check}' passed.", check.Name);
                }

                timeout.Token.ThrowIfCancellationRequested();
                await _messageClientHolder.ReportReadyAsync().WaitAsync(timeout.Token);
            }
            catch (OperationCanceledException e) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException($"Server was not ready within {ReadyTimeout}: {e.Message}", e);
            }
        }

        public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
