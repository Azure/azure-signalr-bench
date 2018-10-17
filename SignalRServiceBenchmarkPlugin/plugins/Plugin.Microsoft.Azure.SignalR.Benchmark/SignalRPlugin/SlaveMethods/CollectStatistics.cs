using Common;
using Plugin.Base;
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
                pluginParameters.TryGetTypedValue($"{SignalRConstants.StatisticsStore}.{type}", out ConcurrentDictionary<string, object> statistics, (obj) => (ConcurrentDictionary<string, object>)obj);

                // Return statistics
                return statistics;
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
