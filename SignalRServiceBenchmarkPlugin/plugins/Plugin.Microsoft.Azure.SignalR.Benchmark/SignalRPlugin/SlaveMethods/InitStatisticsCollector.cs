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
    public class InitStatisticsCollector : ISlaveMethod
    {
        public async Task<IDictionary<string, object>> Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Init statistic collector...");

                // Get parameters
                stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
                stepParameters.TryGetTypedValue($"{SignalRConstants.LatencyStep}.{type}", out long latencyStep, Convert.ToInt64);
                stepParameters.TryGetTypedValue($"{SignalRConstants.LatencyMax}.{type}", out long latencyMax, Convert.ToInt64);

                // Init statistic collector
                pluginParameters[$"{SignalRConstants.StatisticsStore}.{type}"] = new StatisticsCollector(latencyStep, latencyMax);

                return null;
            }
            catch (Exception ex)
            {
                var message = $"Fail to init statistic collector: {ex}";
                Log.Error(message);
                throw;
            }
        }
    }
}
