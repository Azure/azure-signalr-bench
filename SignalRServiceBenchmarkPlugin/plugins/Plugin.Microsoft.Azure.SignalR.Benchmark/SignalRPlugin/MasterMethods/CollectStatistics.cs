using Plugin.Base;
using Rpc.Service;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.MasterMethods
{
    public class CollectStatistics : IMasterMethod
    {
        public Task Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters, IList<IRpcClient> clients)
        {

            Log.Information($"Start to collect statistics...");

            // Get parameters
            PluginUtils.TryGetTypedValue(stepParameters, SignalRConstants.Interval, out double interval, Convert.ToDouble);
            PluginUtils.TryGetTypedValue(stepParameters, SignalRConstants.Type, out string type, Convert.ToString);

            // Start timer
            var timer = new System.Timers.Timer(interval);
            timer.AutoReset = true;
            timer.Elapsed += async (sender, e) =>
            {
                var results = await Task.WhenAll(from client in clients
                                                 select client.QueryAsync(stepParameters));

                // Debug
                foreach (var result in results) Log.Information($"statistics\n{result.GetContents()}");
            };
            timer.Start();

            // Save timer to plugin
            pluginParameters[$"{SignalRConstants.Timer}.{type}"] = timer;

            return Task.CompletedTask;
        }
    }
}
