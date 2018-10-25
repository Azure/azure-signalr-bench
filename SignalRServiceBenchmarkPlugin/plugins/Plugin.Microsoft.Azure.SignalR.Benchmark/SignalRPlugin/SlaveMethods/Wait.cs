using Common;
using Plugin.Base;
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

                await Task.Delay(TimeSpan.FromMilliseconds(duration));

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
