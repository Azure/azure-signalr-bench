using Common;
using Plugin.Base;
using Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods.Statistics;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public class ConditionalStop: ISlaveMethod
    {
        public Task<IDictionary<string, object>> Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Wait...");

                // Get parameters
                stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
                stepParameters.TryGetTypedValue(SignalRConstants.CriteriaMaxFailConnectionPercentage,
                    out double criteriaMaxFailConnectionPercentage, Convert.ToDouble);
                stepParameters.TryGetTypedValue(SignalRConstants.CriteriaMaxFailConnectionAmount,
                    out int criteriaMaxFailConnectionAmount, Convert.ToInt32);

                // Get context
                pluginParameters.TryGetTypedValue($"{SignalRConstants.StatisticsStore}.{type}",
                    out StatisticsCollector statisticsCollector, obj => (StatisticsCollector) obj);

                return Task.FromResult(statisticsCollector.GetData());
            }
            catch (Exception ex)
            {
                var message = $"Fail to conditional stop: {ex}";
                Log.Error(message);
                throw;
            }
        }
    }
}
