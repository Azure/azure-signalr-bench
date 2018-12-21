using Microsoft.AspNetCore.SignalR.Client;
using Plugin.Base;
using Rpc.Service;
using System.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Common;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.MasterMethods
{
    public class Echo : IMasterMethod
    {
        public Task Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters, IList<IRpcClient> clients)
        {
            Log.Information($"Echo...");

            if (stepParameters.TryGetValue(SignalRConstants.Duration, out object value))
            {
                stepParameters.TryGetTypedValue(SignalRConstants.Duration, out long duration, Convert.ToInt64);
                var task = Task.WhenAll(from client in clients select client.QueryAsync(stepParameters));
                return Util.TimeoutCheckedTask(task, duration * 2, nameof(Echo));
            }
            return Task.WhenAll(from client in clients select client.QueryAsync(stepParameters));
        }
    }
}
