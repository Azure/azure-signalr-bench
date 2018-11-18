using Common;
using Plugin.Base;
using Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods.Statistics;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public class CollectStatistics : ISlaveMethod
    {
        public async Task<IDictionary<string, object>> Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Collect statistics...");

                // Get parameters
                stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.StatisticsStore}.{type}", out StatisticsCollector statistics, (obj) => (StatisticsCollector) obj);

                if (pluginParameters.ContainsKey($"{SignalRConstants.ConnectionSuccessFlag}.{type}"))
                {
                    pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionSuccessFlag}.{type}", out List<SignalREnums.ConnectionState> connectionsSuccessFlag, (obj) => (List<SignalREnums.ConnectionState>)obj);
                    // Update connections' states
                    statistics.UpdateConnectionsState(connectionsSuccessFlag);
                }
                else
                {
                    pluginParameters[$"{SignalRConstants.ConnectionSuccessFlag}.{type}"] = null;
                }

                // Return statistics
                return statistics.GetData();
            }
            catch (Exception ex)
            {
                var message = $"Fail to collect statistics: {ex}";
                Log.Error(message);
                throw;
            }
        }
    }
}
