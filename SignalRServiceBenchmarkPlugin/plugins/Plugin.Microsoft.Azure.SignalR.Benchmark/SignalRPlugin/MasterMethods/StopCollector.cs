using Plugin.Base;
using Rpc.Service;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.MasterMethods
{
    public class StopCollector : IMasterMethod
    {
        public Task Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters, IList<IRpcClient> clients)
        {
            Log.Information($"Stop collecting...");

            // Get parameters
            PluginUtils.TryGetTypedValue(stepParameters, SignalRConstants.Type, out string type, Convert.ToString);
            PluginUtils.TryGetTypedValue(pluginParameters, $"{SignalRConstants.Timer}.{type}", out System.Timers.Timer timer, obj => (System.Timers.Timer) obj);

            // Stop and dispose timer
            timer.Stop();
            timer.Dispose();

            return Task.CompletedTask;
        }
    }
}
