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
                MessageHandler.CreateCommandHandler(Roles.Clients, Commands.Clients.StopScenario, StopScenario));
            await _client.ReportReadyAsync(new ReportReadyParameters() { Role = Roles.Clients });
            _logger.LogInformation("Message client handlers inited.");
        }

        private Task Crash(CommandMessage commandMessage)
        {
            _logger.LogWarning("Clients start to crash..");
            Environment.Exit(1);
            return Task.CompletedTask;
        }

        private async Task SetClientRange(CommandMessage commandMessage)
        {
            _logger.LogInformation("Start to set client range: {parameter}",
                JsonConvert.SerializeObject(commandMessage.Parameters));
            var setClientRangeParameters = commandMessage.Parameters?.ToObject<SetClientRangeParameters>();
            if (setClientRangeParameters == null)
            {
                const string error = "Unable to handle range message, parameter cannot be null.";
                _logger.LogError(error);
                await Client.AckFaultedAsync(commandMessage, error);
                return;
            }

            _scenarioState.SetClientRange(setClientRangeParameters);
            _logger.LogInformation("Client range set.");
            await Client.AckCompletedAsync(commandMessage);
            _logger.LogInformation("Client range acked.");
        }

        private async Task StartClientConnections(CommandMessage commandMessage)
        {
            _logger.LogInformation("Start connections: {parameter}",
                JsonConvert.SerializeObject(commandMessage.Parameters));
            var startConnectionsParameters = commandMessage.Parameters?.ToObject<StartClientConnectionsParameters>();
            if (startConnectionsParameters == null)
            {
                const string error = "Unable to handle start client connections message, parameter cannot be null.";
                _logger.LogError(error);
                await Client.AckFaultedAsync(commandMessage, error);
                return;
            }

            await _scenarioState.StartClientConnections(this, startConnectionsParameters);
            await Client.AckCompletedAsync(commandMessage);
            _logger.LogInformation("Start client connections acked.");
        }

        private async Task StopClientConnections(CommandMessage commandMessage)
        {
            _logger.LogInformation("Stop connections: {parameter}",
                JsonConvert.SerializeObject(commandMessage.Parameters));
            var stopConnectionsParameters = commandMessage.Parameters?.ToObject<StopClientConnectionsParameters>();
            if (stopConnectionsParameters == null)
            {
                const string error = "Unable to handle stop client connections message, parameter cannot be null.";
                _logger.LogError(error);
                await Client.AckFaultedAsync(commandMessage, error);
                return;
            }

            await _scenarioState.StopClientConnections(stopConnectionsParameters);
            await Client.AckCompletedAsync(commandMessage);
            _logger.LogInformation("Stop client connections acked.");
        }

        private async Task SetScenario(CommandMessage commandMessage)
        {
            try
            {
                var param = JsonConvert.SerializeObject(commandMessage.Parameters);
                _logger.LogInformation($"Start to set scenario: {param}");
                var setSenarioParameters = commandMessage.Parameters?.ToObject<SetScenarioParameters>();
                if (setSenarioParameters == null)
                {
                    const string error = "Unable to handle set scenario message, parameter cannot be null.";
                    _logger.LogError(error);
                    await Client.AckFaultedAsync(commandMessage, error);
                    return;
                }

                _scenarioState.SetSenario(setSenarioParameters);
                await Client.AckCompletedAsync(commandMessage);
                _logger.LogInformation("Set scenario acked.");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Set scenario error");
            }
        }

        private async Task StartScenario(CommandMessage commandMessage)
        {
            _logger.LogInformation("Start scenario: {parameter}",
                JsonConvert.SerializeObject(commandMessage.Parameters));
            var startScenarioParameters = commandMessage.Parameters?.ToObject<StartScenarioParameters>();
            if (startScenarioParameters == null)
            {
                const string error = "Unable to handle start scenario message, parameter cannot be null.";
                _logger.LogError(error);
                await Client.AckFaultedAsync(commandMessage, error);
                return;
            }

            _scenarioState.StartSenario(startScenarioParameters);
            await Client.AckCompletedAsync(commandMessage);
            _logger.LogInformation("Start scenario acked.");
        }

        private async Task StopScenario(CommandMessage commandMessage)
        {
            _logger.LogInformation("Stop scenario: {parameter}",
                JsonConvert.SerializeObject(commandMessage.Parameters));
            var stopScenarioParameters = commandMessage.Parameters?.ToObject<StopScenarioParameters>();
            if (stopScenarioParameters == null)
            {
                const string error = "Unable to handle stop scenario message, parameter cannot be null.";
                _logger.LogError(error);
                await Client.AckFaultedAsync(commandMessage, error);
                return;
            }

            _scenarioState.StopSenario(stopScenarioParameters);
            await Client.AckCompletedAsync(commandMessage);
        }
    }
}