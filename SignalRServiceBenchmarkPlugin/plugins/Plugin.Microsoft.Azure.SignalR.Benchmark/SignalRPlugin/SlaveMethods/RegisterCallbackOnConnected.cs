using Common;
using Plugin.Base;
using Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods.Statistics;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public class RegisterCallbackOnConnected : ISlaveMethod
    {
        public Task<IDictionary<string, object>> Do(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Set callback for receiving onconnected notification...");

                // Get parameters
                stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionStore}.{type}",
                    out IList<IHubConnectionAdapter> connections, (obj) => (IList<IHubConnectionAdapter>)obj);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.StatisticsStore}.{type}",
                    out var statisticsCollector, obj => (StatisticsCollector)obj);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.RegisteredCallbacks}.{type}",
                    out var registeredCallbacks, obj => (IList<Action<IList<IHubConnectionAdapter>, StatisticsCollector, string>>)obj);
                // Set callback
                RegisterCallbackBase.SetCallbackOnConnected(connections, statisticsCollector, SignalRConstants.OnConnectedCallback);
                registeredCallbacks.Add(RegisterCallbackBase.SetCallbackOnConnected);

                return Task.FromResult<IDictionary<string, object>>(null);
            }
            catch (Exception ex)
            {
                var message = $"Fail to set callback for receiving onconnected notification: {ex}";
                Log.Error(message);
                throw;
            }
        }
    }
}
