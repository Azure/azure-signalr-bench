using Common;
using Plugin.Base;
using Plugin.Microsoft.Azure.SignalR.Benchmark.AgentMethods.Statistics;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.AgentMethods
{
    public class RegisterCallbackRecordLatency: RegisterCallbackBase, IAgentMethod
    {
        public Task<IDictionary<string, object>> Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Set callback for recording latency...");

                // Get parameters
                stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionStore}.{type}",
                    out IList<IHubConnectionAdapter> connections, (obj) => (IList<IHubConnectionAdapter>)obj);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.StatisticsStore}.{type}",
                    out var statisticsCollector, obj => (StatisticsCollector)obj);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.RegisteredCallbacks}.{type}",
                    out var registeredCallbacks, obj => (IList<Action<IList<IHubConnectionAdapter>, StatisticsCollector>>)obj);

                // Set callback
                SetCallback(connections, statisticsCollector);
                registeredCallbacks.Add(SetCallback);

                return Task.FromResult<IDictionary<string, object>>(null);
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
