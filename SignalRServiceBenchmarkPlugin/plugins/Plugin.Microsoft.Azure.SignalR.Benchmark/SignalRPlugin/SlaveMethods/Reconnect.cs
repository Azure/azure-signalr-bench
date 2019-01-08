using Common;
using Microsoft.AspNetCore.SignalR.Client;
using Plugin.Base;
using Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods.Statistics;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public class Reconnect: ISlaveMethod
    {
        public async Task<IDictionary<string, object>> Do(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters)
        {
            try
            {
                // Get parameters
                stepParameters.TryGetTypedValue(SignalRConstants.HubUrls, out string urls, Convert.ToString);
                stepParameters.TryGetTypedValue(SignalRConstants.HubProtocol, out string protocol, Convert.ToString);
                stepParameters.TryGetTypedValue(SignalRConstants.TransportType, out string transportType, Convert.ToString);
                stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
                stepParameters.TryGetTypedValue(SignalRConstants.ConcurrentConnection, out int concurrentConnection, Convert.ToInt32);

                // Get context
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionStore}.{type}",
                    out IList<IHubConnectionAdapter> connections, (obj) => (IList<IHubConnectionAdapter>)obj);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionSuccessFlag}.{type}",
                    out List<SignalREnums.ConnectionState> connectionsSuccessFlag, (obj) => (List<SignalREnums.ConnectionState>)obj);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionIndex}.{type}",
                    out List<int> connectionIndex, (obj) => (List<int>)obj);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.RegisteredCallbacks}.{type}",
                    out var registeredCallbacks, obj => (IList<Action<IList<IHubConnectionAdapter>, StatisticsCollector, string>>)obj);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.StatisticsStore}.{type}",
                    out StatisticsCollector statisticsCollector, obj => (StatisticsCollector)obj);

                // Re-create broken connections
                var newConnections = await RecreateBrokenConnections(
                    connections, connectionIndex,
                    connectionsSuccessFlag, urls,
                    transportType, protocol,
                    SignalRConstants.ConnectionCloseTimeout);

                // Start connections
                var packages = (from i in Enumerable.Range(0, connections.Count())
                                select (Connection: connections[i], LocalIndex: i,
                                ConnectionsSuccessFlag: connectionsSuccessFlag,
                                NormalState: SignalREnums.ConnectionState.Reconnect,
                                AbnormalState: SignalREnums.ConnectionState.Fail)).ToList();
                await Task.WhenAll(Util.BatchProcess(packages, SignalRUtils.StartConnect, concurrentConnection));

                // Re-setCallbacks
                foreach (var registerCallback in registeredCallbacks)
                {
                    registerCallback(newConnections, statisticsCollector, SignalRConstants.RecordLatencyCallbackName);
                }

                return null;
            }
            catch (Exception ex)
            {
                var message = $"Fail to reconnect: {ex}";
                Log.Error(message);
                throw;
            }
        }

        private async Task<IList<IHubConnectionAdapter>> RecreateBrokenConnections(
            IList<IHubConnectionAdapter> connections,
            IList<int> connectionIndex,
            IList<SignalREnums.ConnectionState> connectionsSuccessFlag,
            string urls,
            string transportTypeString,
            string protocolString,
            int closeTimeout)
        {
            // Filter broken connections and local index
            var packages = (from i in Enumerable.Range(0, connections.Count)
                            where connectionsSuccessFlag[i] == SignalREnums.ConnectionState.Fail
                            select new { Connection = connections[i], LocalIndex = i, GlobalIndex = connectionIndex[i]}).ToList();

            // Destroy broken connections
            foreach (var package in packages)
            {
                await package.Connection.StopAsync();
                await package.Connection.DisposeAsync();
            }

            // Re-create connections
            var newConnections = SignalRUtils.CreateConnections(
                (from pkg in packages select pkg.GlobalIndex).ToList(),
                urls, transportTypeString, protocolString, closeTimeout);

            // Setup connection drop handler
            SignalRUtils.SetConnectionOnClose(connections, connectionsSuccessFlag);

            // Map new connections to orignal connection list
            for (var i = 0; i < newConnections.Count; i++)
            {
                connections[packages[i].LocalIndex] = newConnections[i];
            }

            for (var i = 0; i < newConnections.Count; i++)
            {
                connectionsSuccessFlag[packages[i].LocalIndex] = SignalREnums.ConnectionState.Init;
            }

            return newConnections;
        }
    }
}
