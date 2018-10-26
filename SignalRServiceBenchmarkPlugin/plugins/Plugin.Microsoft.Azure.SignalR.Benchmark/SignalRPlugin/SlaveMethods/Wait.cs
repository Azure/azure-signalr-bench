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
    public class Wait : ISlaveMethod
    {
        public async Task<IDictionary<string, object>> Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Wait...");

                // Get parameters
                stepParameters.TryGetTypedValue(SignalRConstants.Duration, out long duration, Convert.ToInt64);
                stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.StatisticsStore}.{type}", out var statisticsCollector, obj => (StatisticsCollector)obj);

                await Task.Delay(TimeSpan.FromMilliseconds(duration));

                // Update epoch at the end of 'Wait' to ensure all the messages are received and all clients stop sending
                statisticsCollector.IncreaseEpoch();

                return null;
            }
            catch (Exception ex)
            {
                var message = $"Fail to wait: {ex}";
                Log.Error(message);
                throw;
            }
        }
    }
}
