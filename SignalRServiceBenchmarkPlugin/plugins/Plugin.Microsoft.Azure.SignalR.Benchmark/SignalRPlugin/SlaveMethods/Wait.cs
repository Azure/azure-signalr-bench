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
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionSuccessFlag}.{type}", out List<SignalREnums.ConnectionState> connectionsSuccessFlag, (obj) => (List<SignalREnums.ConnectionState>)obj);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.StatisticsStore}.{type}", out var statisticsCollector, obj => (StatisticsCollector)obj);

                await Task.Delay(TimeSpan.FromMilliseconds(duration));

                // Update for reconnect statistics: change flag from reconnect to success
                SignalRUtils.ChangeFlagConnectionFlag(connectionsSuccessFlag);
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
