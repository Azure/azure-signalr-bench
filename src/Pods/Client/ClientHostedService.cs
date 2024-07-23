// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Azure.SignalRBench.Common;
using Azure.SignalRBench.Coordinator.Entities;
using Azure.SignalRBench.Storage;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Azure.SignalRBench.Client
{
    public class ClientHostedService : IHostedService
    {
        private readonly IConfiguration _configuration;
        private readonly MessageClientHolder _messageClientHolder;
        private readonly AzureEventSourceLogForwarder _forwarder;
        private readonly IPerfStorage _perfStorage;
        private readonly IScenarioState _scenarioState;
        private readonly ILogger<ClientHostedService> _logger;

        public ClientHostedService(IConfiguration configuration, IScenarioState scenarioState, MessageClientHolder messageClientHolder, IPerfStorage perfStorage, ILoggerFactory loggerFactory)
        {
            _configuration = configuration;
            _messageClientHolder = messageClientHolder;
            _perfStorage = perfStorage;
            _logger = loggerFactory.CreateLogger<ClientHostedService>();
            _scenarioState = scenarioState;
            _forwarder = new AzureEventSourceLogForwarder(loggerFactory);
            _forwarder.Start();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _messageClientHolder.InitializeAsync(
                _configuration[PerfConstants.ConfigurationKeys.TestIdKey],
                _configuration[PerfConstants.ConfigurationKeys.RedisConnectionStringKey],
                _configuration[PerfConstants.ConfigurationKeys.PodNameStringKey]);
            await StartLongRunAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) =>
            Task.CompletedTask;
        
        private async Task StartLongRunAsync(CancellationToken cancellationToken)
        {
            var partitionKey = _configuration[PerfConstants.ConfigurationKeys.TestStatusPartitionKey];
            var rowKey = _configuration[PerfConstants.ConfigurationKeys.TestStatusRowKey];
            var podId = _configuration[PerfConstants.ConfigurationKeys.PodNameStringKey];
            _logger.LogInformation("partitionKey: {partitionKey}, rowKey: {rowKey}, podId: {podId}", partitionKey, rowKey, podId);
            var _testStatusAccessor =
                await _perfStorage.GetTableAsync<TestStatusEntity>(PerfConstants.TableNames.TestStatus);
            var testStatusEntity = await _testStatusAccessor.GetAsync(partitionKey, rowKey);
            if (testStatusEntity.JobState == TestState.Longrun.ToString())
            {
                _logger.LogInformation("Start long run with command {command}", testStatusEntity.LongRunContext);
                var commandHistory = JsonConvert.DeserializeObject<ClientCommandHistory>(testStatusEntity.LongRunContext);
                _logger.LogInformation("Long run history parsed");
                for(int i=0;i<commandHistory.Histories.Count;i++)
                {
                   var round = commandHistory.Histories[i];
                   var setClientRange = round.SetClientRangeParameters[podId];
                   _scenarioState.SetClientRange(setClientRange);
                   _logger.LogInformation("Set client range");
                   var startClientConnections = round.StartClientConnectionsParameters;
                   _scenarioState.StartClientConnections(_messageClientHolder,startClientConnections);
                   _logger.LogInformation("Start client connections");
                   var setScenario = round.SetScenarioParameters;
                   _scenarioState.SetSenario(setScenario);
                   _logger.LogInformation("Set scenario");
                   var startScenario = round.StartScenarioParameters;
                   _scenarioState.StartSenario(startScenario);
                   _logger.LogInformation("Start scenario");
                   var stopScenario = round.StopScenarioParameters;
                   _scenarioState.StopSenario(stopScenario);
                   _logger.LogInformation("Stop scenario");
                }
                
                // Start long run
                _scenarioState.StartSenario(commandHistory.Histories.Last().StartScenarioParameters);
                _logger.LogInformation("Long run started");
            }
        }
    }
}
