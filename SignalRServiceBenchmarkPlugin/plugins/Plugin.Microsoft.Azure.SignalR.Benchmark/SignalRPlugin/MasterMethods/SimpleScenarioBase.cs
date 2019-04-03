using Common;
using Rpc.Service;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.MasterMethods
{
    public class SimpleScenarioBase
    {
        public Task Run(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters,
            IList<IRpcClient> clients)
        {
            Log.Information($"{GetType().Name}...");

            if (stepParameters.TryGetValue(SignalRConstants.Duration, out object value))
            {
                stepParameters.TryGetTypedValue(SignalRConstants.Duration, out long duration, Convert.ToInt64);
                var task = Task.WhenAll(from client in clients select client.QueryAsync(stepParameters));
                return Util.TimeoutCheckedTask(task, duration * 5, GetType().Name);
            }
            return Task.WhenAll(from client in clients select client.QueryAsync(stepParameters));
        }
    }
}
