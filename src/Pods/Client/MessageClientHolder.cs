﻿using Azure.SignalRBench.Common;
using Azure.SignalRBench.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Azure.SignalRBench.Client
{
    public class MessageClientHolder
    {
        public static MessageClient Client { get; private set; }
        private ILogger<MessageClientHolder> _logger;
        private IScenarioState _scenarioState;

        public MessageClientHolder(IConfiguration configuration, ILogger<MessageClientHolder> logger, IScenarioState scenarioState)
        {
            _logger = logger;
            _scenarioState = scenarioState;
            AddMessageHandlers(configuration[Constants.EnvVariableKey.RedisConnectionStringKey]);
        }

        private void AddMessageHandlers(string connectionString)
        {
            var crash = CrashHandler();
            var setClientRange = SetClientRangeHandler();
            var startConnections = StartConnectionsHandler();
            var stopConnections = StopConnectionsHandler();
            var setScenario = SetScenarioHandler();
            var startScenario = StartScenarioHandler();
            var stopScenario = StopScenarioHandler();
            Task.Run(async () => Client = await MessageClient.ConnectAsync(connectionString, Roles.AppServers, crash, setClientRange, startConnections, stopConnections, setScenario, startScenario, stopScenario));
        }

        private MessageHandler CrashHandler()
           => MessageHandler.CreateCommandHandler(Commands.General.Crash, cmd =>
           {
               _logger.LogWarning("AppServer start to crash..");
               Environment.Exit(1);
               return Task.CompletedTask;
           });

        private MessageHandler SetClientRangeHandler()
          => MessageHandler.CreateCommandHandler(Commands.Clients.SetClientRange, async cmd =>
           {
               _logger.LogInformation("Start to set client range..");
               var setClientRangeParameters = cmd.Parameters?.ToObject<SetClientRangeParameters>();
               _scenarioState.SetClientRange(setClientRangeParameters);
               await Client.AckAsync(cmd, true);
           });

        private MessageHandler StartConnectionsHandler()
          => MessageHandler.CreateCommandHandler(Commands.Clients.StartClientConnections, async cmd =>
          {
              _logger.LogWarning("Start connections..");
              var startConnectionsParameters = cmd.Parameters?.ToObject<StartClientConnectionsParameters>();
              await _scenarioState.StartClientConnections(startConnectionsParameters);
              await Client.AckAsync(cmd, true);
          });

        private MessageHandler StopConnectionsHandler()
          => MessageHandler.CreateCommandHandler(Commands.Clients.StopClientConnections, async cmd =>
          {
              _logger.LogWarning("Stop connections..");
              var stopConnectionsParameters = cmd.Parameters?.ToObject<StopClientConnectionsParameters>();
              await _scenarioState.StopClientConnections(stopConnectionsParameters);
              await Client.AckAsync(cmd, true);
          });

        private MessageHandler SetScenarioHandler()
          => MessageHandler.CreateCommandHandler(Commands.Clients.SetScenario, async cmd =>
           {
               _logger.LogInformation("Start to set scenario..");
               var setSenarioParameters = cmd.Parameters?.ToObject<SetScenarioParameters>();
               _scenarioState.SetSenario(setSenarioParameters);
               await Client.AckAsync(cmd, true);
           });

        private MessageHandler StartScenarioHandler()
          => MessageHandler.CreateCommandHandler(Commands.Clients.StartClientConnections, async cmd =>
          {
              _logger.LogWarning("Start scenario..");
              var startScenarioParameters = cmd.Parameters?.ToObject<StartScenarioParameters>();
              await _scenarioState.StartSenario(startScenarioParameters);
              await Client.AckAsync(cmd, true);
          });

        private MessageHandler StopScenarioHandler()
          => MessageHandler.CreateCommandHandler(Commands.Clients.StartClientConnections, async cmd =>
          {
              _logger.LogWarning("Stop scenario..");
              var stopScenarioParameters = cmd.Parameters?.ToObject<StopScenarioParameters>();
              await _scenarioState.StopSenario(stopScenarioParameters);
              await Client.AckAsync(cmd, true);
          });
    }
}
