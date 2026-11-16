// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Messages;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Azure.SignalRBench.Client
{
    public class MessageClientHolder
    {
        private readonly IScenarioState _scenarioState;
        private readonly ILogger<MessageClientHolder> _logger;

        private MessageClient? _client;
        private MessageClient? _timeCoordinatorClient;

        public MessageClientHolder(IScenarioState scenarioState, ILogger<MessageClientHolder> logger)
        {
            _scenarioState = scenarioState;
            _logger = logger;
        }

        public MessageClient Client => _client ?? throw new InvalidOperationException();

        public async Task InitializeAsync(string testId, string connectionString, string podName)
        {
            if (_client != null)
            {
                throw new InvalidOperationException();
            }

            _client = await MessageClient.ConnectAsync(connectionString, testId, podName);
            await _client.WithHandlers(
                MessageHandler.CreateCommandHandler(Commands.General.Crash, Crash),
                MessageHandler.CreateCommandHandler(Roles.Clients, Commands.General.Crash, Crash),
                MessageHandler.CreateCommandHandler(Commands.Clients.SetClientRange, SetClientRange),
                MessageHandler.CreateCommandHandler(Roles.Clients, Commands.Clients.StartClientConnections,
                    StartClientConnections),
                MessageHandler.CreateCommandHandler(Roles.Clients, Commands.Clients.StopClientConnections,
                    StopClientConnections),
                MessageHandler.CreateCommandHandler(Roles.Clients, Commands.Clients.SetScenario, SetScenario),
                MessageHandler.CreateCommandHandler(Roles.Clients, Commands.Clients.StartScenario, StartScenario),
                MessageHandler.CreateCommandHandler(Roles.Clients, Commands.Clients.StopScenario, StopScenario)
               );
            await _client.ReportReadyAsync(new ReportReadyParameters() { Role = Roles.Clients });
            _logger.LogInformation("Message client handlers inited.");
            
            _timeCoordinatorClient = await MessageClient.ConnectAsync(connectionString, PerfConstants.Channel.All, podName);
            await _timeCoordinatorClient.WithHandlers(
                MessageHandler.CreateCommandHandler(Roles.Clients, Commands.Clients.SetCoordinatorTime, SetCoordinatorTime));
        }

        private Task Crash(CommandMessage commandMessage)
        {
            _logger.LogWarning("Clients start to crash..");
            Environment.Exit(1);
            return Task.CompletedTask;
        }

        private Task SetClientRange(CommandMessage commandMessage) =>
            ExecuteCommandAsync<SetClientRangeParameters>(commandMessage, parameters =>
            {
                _scenarioState.SetClientRange(parameters);
                return Task.CompletedTask;
            });

        private Task StartClientConnections(CommandMessage commandMessage) =>
            ExecuteCommandAsync<StartClientConnectionsParameters>(commandMessage,
                parameters => _scenarioState.StartClientConnections(this, parameters));

        private Task StopClientConnections(CommandMessage commandMessage) =>
            ExecuteCommandAsync<StopClientConnectionsParameters>(commandMessage,
                parameters => _scenarioState.StopClientConnections(parameters));

        private Task SetScenario(CommandMessage commandMessage) =>
            ExecuteCommandAsync<SetScenarioParameters>(commandMessage, parameters =>
            {
                _scenarioState.SetSenario(parameters);
                return Task.CompletedTask;
            });

        private Task StartScenario(CommandMessage commandMessage) =>
            ExecuteCommandAsync<StartScenarioParameters>(commandMessage, parameters =>
            {
                _scenarioState.StartSenario(parameters);
                return Task.CompletedTask;
            });
        
        private async Task SetCoordinatorTime(CommandMessage commandMessage)
        {
            _logger.LogInformation("Set CoordinatorTime: {parameter}",
                JsonConvert.SerializeObject(commandMessage.Parameters));
            var setCoordinatorTimeParameters = commandMessage.Parameters?.ToObject<SetCoordinatorTimeParameters>();
            if (setCoordinatorTimeParameters == null)
            {
                const string error = "Unable to handle set coordinatorTime message, parameter cannot be null.";
                _logger.LogError(error);
                return;
            }
            ClientAgentContext.CoordinatorTime(setCoordinatorTimeParameters.CoordinatorTime);
        }

        private Task StopScenario(CommandMessage commandMessage) =>
            ExecuteCommandAsync<StopScenarioParameters>(commandMessage,
                parameters => _scenarioState.StopSenario(parameters));

        private async Task ExecuteCommandAsync<T>(CommandMessage commandMessage, Func<T, Task> execute)
            where T : class
        {
            var client = Client;
            try
            {
                _logger.LogInformation("Executing client command {command}.", commandMessage.Command);
                var parameters = commandMessage.Parameters?.ToObject<T>()
                    ?? throw new ArgumentException("Command parameters cannot be null.");
                await execute(parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Client command {command} failed.", commandMessage.Command);
                await client.AckFaultedAsync(commandMessage, ex.Message);
                return;
            }
            await client.AckCompletedAsync(commandMessage);
        }
    }
}