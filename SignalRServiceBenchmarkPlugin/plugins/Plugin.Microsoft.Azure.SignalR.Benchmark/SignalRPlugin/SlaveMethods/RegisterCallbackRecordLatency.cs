using Common;
using Microsoft.AspNetCore.SignalR.Client;
using Plugin.Base;
using Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods.Statistics;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public class RegisterCallbackRecordLatency: RegisterCallbackBase, ISlaveMethod
    {
        public async Task<IDictionary<string, object>> Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Set callback for recording latency...");

                // Get parameters
                stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionStore}.{type}", out IList<HubConnection> connections, (obj) => (IList<HubConnection>)obj);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.StatisticsStore}.{type}", out var statisticsCollector, obj => (StatisticsCollector)obj);

                // Set callback
                SetCallback(connections, SignalRConstants.RecordLatencyCallbackName, statisticsCollector);

                return null;
            }
            catch (Exception ex)
            {
                var message = $"Fail to set callback for recording latency: {ex}";
                Log.Error(message);
                throw;
            }
        }        
    }
}
