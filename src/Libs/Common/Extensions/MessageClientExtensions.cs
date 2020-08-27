// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.SignalRBench.Common;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace Azure.SignalRBench.Messages
{
    public static class MessageClientExtensions
    {
        #region MyRegion

        public static async Task<CommandMessage> CrashAsync(this IMessageClient client, string instanceId)
        {
            var message = new CommandMessage { Command = Commands.General.Crash };
            await client.SendCommandAsync(instanceId, message);
            return message;
        }

        public static Task<CommandMessage> CrashClientsAsync(this IMessageClient client) =>
            CrashAsync(client, Roles.Clients);

        public static Task<CommandMessage> CrashAppServersAsync(this IMessageClient client) =>
            CrashAsync(client, Roles.AppServers);

        #endregion

        #region Commands To Clients

        public static async Task<CommandMessage> StartClientConnectionsAsync(this IMessageClient client, StartClientConnectionsParameters parameters)
        {
            var message = new CommandMessage { Command = Commands.Clients.StartClientConnections, Parameters = JObject.FromObject(parameters) };
            await client.SendCommandAsync(Roles.Clients, message);
            return message;
        }

        public static async Task<CommandMessage> StopClientConnectionsAsync(this IMessageClient client, StopClientConnectionsParameters parameters)
        {
            var message = new CommandMessage { Command = Commands.Clients.StopClientConnections, Parameters = JObject.FromObject(parameters) };
            await client.SendCommandAsync(Roles.Clients, message);
            return message;
        }

        public static async Task<CommandMessage> SetScenarioAsync(this IMessageClient client, SetScenarioParameters parameters)
        {
            var message = new CommandMessage { Command = Commands.Clients.SetScenario, Parameters = JObject.FromObject(parameters) };
            await client.SendCommandAsync(Roles.Clients, message);
            return message;
        }

        public static async Task<CommandMessage> StartScenarioAsync(this IMessageClient client, StartScenarioParameters parameters)
        {
            var message = new CommandMessage { Command = Commands.Clients.StartScenario, Parameters = JObject.FromObject(parameters) };
            await client.SendCommandAsync(Roles.Clients, message);
            return message;
        }

        public static async Task<CommandMessage> StopScenarioAsync(this IMessageClient client, StopScenarioParameters parameters)
        {
            var message = new CommandMessage { Command = Commands.Clients.StopScenario, Parameters = JObject.FromObject(parameters) };
            await client.SendCommandAsync(Roles.Clients, message);
            return message;
        }

        public static async Task<CommandMessage> SetClientRangeAsync(this IMessageClient client, string clientInstance, SetClientRangeParameters parameters)
        {
            var message = new CommandMessage { Command = Commands.Clients.SetClientRange, Parameters = JObject.FromObject(parameters) };
            await client.SendCommandAsync(clientInstance, message);
            return message;
        }

        #endregion

        #region Commands To AppServer

        public static async Task<CommandMessage> AppServerGracefulShutdownThenRestartAsync(this IMessageClient client, string serverId)
        {
            var message = new CommandMessage { Command = Commands.AppServer.GracefulShutdownThenRestart };
            await client.SendCommandAsync(serverId, message);
            return message;
        }

        #endregion

        #region Commands To Coordinator

        public static async Task<CommandMessage> ReportReadyAsync(this IMessageClient client, ReportReadyParameters parameters)
        {
            var message = new CommandMessage { Command = Commands.Coordinator.ReportReady, Parameters = JObject.FromObject(parameters) };
            await client.SendCommandAsync(Roles.Coordinator, message);
            return message;
        }

        public static async Task<CommandMessage> ReportClientStatusAsync(this IMessageClient client, ReportClientStatusParameters parameters)
        {
            var message = new CommandMessage { Command = Commands.Coordinator.ReportClientStatus, Parameters = JObject.FromObject(parameters) };
            await client.SendCommandAsync(Roles.Coordinator, message);
            return message;
        }

        #endregion

        #region Ack

        public static Task AckCompletedAsync(this IMessageClient client, CommandMessage command) =>
            client.AckAsync(command, true);

        public static Task AckProgressAsync(this IMessageClient client, CommandMessage command, double progress) =>
            client.AckAsync(command, false, progress);

        #endregion
    }
}
