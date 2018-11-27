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
    public class RegisterCallbackLeaveGroup : RegisterCallbackBase, ISlaveMethod
    {
        public Task<IDictionary<string, object>> Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Set callback for leaving group...");

                // Get parameters
                stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionStore}.{type}",
                    out IList<IHubConnectionAdapter> connections,
                    (obj) => (IList<IHubConnectionAdapter>)obj);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.StatisticsStore}.{type}",
                    out var statisticsCollector, obj => (StatisticsCollector)obj);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.RegisteredCallbacks}.{type}",
                    out var registeredCallbacks,
                    obj => (IList<Action<IList<IHubConnectionAdapter>, StatisticsCollector, string>>)obj);

                // Set callback
                SetCallbackLeaveGroup(connections, statisticsCollector);
                registeredCallbacks.Add(SetCallbackLeaveGroup);

                return null;
            }
            catch (Exception ex)
            {
                var message = $"Fail to set callback for leaving group: {ex}";
                Log.Error(message);
                throw;
            }
        }        
    }
}
