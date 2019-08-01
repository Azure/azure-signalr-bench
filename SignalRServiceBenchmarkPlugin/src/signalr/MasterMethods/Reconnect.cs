using Plugin.Base;
using Rpc.Service;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.MasterMethods
{
    public class Reconnect : ReconnectBase, IMasterMethod
    {
        public Task Do(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters,
            IList<IRpcClient> clients)
        {
            Log.Information($"Reconnect connections...");

            return Reconnect(stepParameters, pluginParameters, clients);
        }
    }
}
