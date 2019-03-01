using Common;
using Plugin.Base;
using Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods.Statistics;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public class StartConnection : BatchConnectionBase, ISlaveMethod
    {
        private StatisticsCollector _statisticsCollector;

        public async Task<IDictionary<string, object>> Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Start connections...");

                // Get parameters
                stepParameters.TryGetTypedValue(SignalRConstants.ConcurrentConnection,
                    out int concurrentConnection, Convert.ToInt32);
                stepParameters.TryGetTypedValue(SignalRConstants.Type,
                    out string type, Convert.ToString);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionStore}.{type}",
                    out IList<IHubConnectionAdapter> connections, (obj) => (IList<IHubConnectionAdapter>)obj);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionSuccessFlag}.{type}",
                    out List<SignalREnums.ConnectionState> connectionsSuccessFlag,
                    (obj) => (List<SignalREnums.ConnectionState>)obj);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.StatisticsStore}.{type}",
                    out _statisticsCollector, obj => (StatisticsCollector)obj);

                // The following get connection Id needs the concurrent connection value
                pluginParameters.TryAdd(SignalRConstants.ConcurrentConnection, concurrentConnection);

                await BatchConnect(
                    stepParameters,
                    pluginParameters,
                    connections,
                    concurrentConnection,
                    connectionsSuccessFlag);
                Log.Information($"Finish starting connection {connections.Count}");
                return null;
            }
            catch (Exception ex)
            {
                var message = $"Fail to start connections: {ex}";
                Log.Error(message);
                throw;
            }
        }
    }
}
