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

            // Get parameters
            PluginUtils.TryGetTypedValue(stepParameters, SignalRConstants.Type, out string type, Convert.ToString);
            PluginUtils.TryGetTypedValue(stepParameters, SignalRConstants.Duration, out double duration, Convert.ToDouble);
            PluginUtils.TryGetTypedValue(stepParameters, SignalRConstants.Interval, out double interval, Convert.ToDouble);
            PluginUtils.TryGetTypedValue(stepParameters, SignalRConstants.RemainderBegin, out int remainderBegin, Convert.ToInt32);
            PluginUtils.TryGetTypedValue(stepParameters, SignalRConstants.RemainderEnd, out int remainderEnd, Convert.ToInt32);
            PluginUtils.TryGetTypedValue(stepParameters, SignalRConstants.Modulo, out int modulo, Convert.ToInt32);

            // Process on clients
            return Task.WhenAll(from client in clients select client.QueryAsync(stepParameters));
        }
    }
}
