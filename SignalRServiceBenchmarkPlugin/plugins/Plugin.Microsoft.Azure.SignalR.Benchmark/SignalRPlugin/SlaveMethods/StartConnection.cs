using Common;
using Microsoft.AspNetCore.SignalR.Client;
using Plugin.Base;
using Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods.Statistics;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public class StartConnection : ISlaveMethod
    {
        private StatisticsCollector _statisticsCollector;

        public async Task<IDictionary<string, object>> Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Start connections...");

                // Get parameters
                stepParameters.TryGetTypedValue(SignalRConstants.ConcurrentConnection, out int concurrentConnection, Convert.ToInt32);
                stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionStore}.{type}", out IList<HubConnection> connections, (obj) => (IList<HubConnection>)obj);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.StatisticsStore}.{type}", out _statisticsCollector, obj => (StatisticsCollector)obj);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionSuccessFlag}.{type}", out List<SignalREnums.ConnectionState> connectionsSuccessFlag, (obj) => (List<SignalREnums.ConnectionState>)obj);

                var packages = (from i in Enumerable.Range(0, connections.Count())
                                select (Connection: connections[i], LocalIndex: i, ConnectionsSuccessFlag: connectionsSuccessFlag)).ToList();

                await Task.WhenAll(Util.BatchProcess(packages, StartConnect, concurrentConnection));

                return null;
            }
            catch (Exception ex)
            {
                var message = $"Fail to start connections: {ex}";
                Log.Error(message);
                throw;
            }
        }

        private async Task StartConnect((HubConnection Connection, int LocalIndex, List<SignalREnums.ConnectionState> connectionsSuccessFlag) package)
        {
            if (package.Connection == null || package.connectionsSuccessFlag[package.LocalIndex] != SignalREnums.ConnectionState.Init) return;

            try
            {
                await package.Connection.StartAsync();
                package.connectionsSuccessFlag[package.LocalIndex] = SignalREnums.ConnectionState.Success;
            }
            catch (Exception ex)
            {
                package.connectionsSuccessFlag[package.LocalIndex] = SignalREnums.ConnectionState.Fail;
                var message = $"Fail to start connection: {ex}";
                Log.Error(message);
                throw;
            }
        }
    }
}
