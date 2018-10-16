using Plugin.Base;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public class InitStatisticsCollector : ISlaveMethod
    {
        public async Task<IDictionary<string, object>> Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Init statistic collector...");

                // Get parameters
                PluginUtils.TryGetTypedValue(stepParameters, SignalRConstants.Type, out string type, Convert.ToString);

                // Init statistic collector
                pluginParameters[$"{SignalRConstants.StatisticsStore}.{type}"] = new ConcurrentDictionary<string, object>();

                return null;
            }
            catch (Exception ex)
            {
                var message = $"Fail to init statistic collector: {ex}";
                Log.Error(message);
                throw new Exception(message);
            }
        }
    }
}
