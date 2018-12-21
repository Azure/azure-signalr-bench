using Common;
using Plugin.Base;
using Rpc.Service;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.MasterMethods
{
    public class Broadcast : IMasterMethod
    {
        public Task Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters, IList<IRpcClient> clients)
        {
            Log.Information($"Broadcast...");

            // Process on clients
            if (stepParameters.TryGetValue(SignalRConstants.Duration, out object value))
            {
                stepParameters.TryGetTypedValue(SignalRConstants.Duration, out long duration, Convert.ToInt64);
                var task = Task.WhenAll(from client in clients select client.QueryAsync(stepParameters));
                return Util.TimeoutCheckedTask(task, duration * 2, nameof(Broadcast));
            }
            return Task.WhenAll(from client in clients select client.QueryAsync(stepParameters));
        }
    }
}
