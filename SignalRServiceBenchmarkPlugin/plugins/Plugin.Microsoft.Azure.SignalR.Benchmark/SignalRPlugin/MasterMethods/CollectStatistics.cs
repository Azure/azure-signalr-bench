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
            stepParameters.TryGetTypedValue(SignalRConstants.Interval, out long interval, Convert.ToInt64);
            stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);

            // Start timer
            var timer = new System.Timers.Timer(interval);
            timer.Elapsed += async (sender, e) =>
            {
                var results = await Task.WhenAll(from client in clients
                                                 select client.QueryAsync(stepParameters));

                // DEBUG
                for (var i = 0; i < results.Count(); i++)
                {
                    Log.Information($"Type: {type} Client: {i}th statistics{Environment.NewLine}{results[i].GetContents()}");
                }
            };
            timer.Start();

            // Save timer to plugin
            pluginParameters[$"{SignalRConstants.Timer}.{type}"] = timer;

            return Task.CompletedTask;
        }
    }
}
