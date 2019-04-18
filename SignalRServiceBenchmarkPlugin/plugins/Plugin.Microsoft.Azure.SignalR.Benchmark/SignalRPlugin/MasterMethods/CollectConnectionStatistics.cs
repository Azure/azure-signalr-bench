using Plugin.Base;
using Rpc.Service;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.MasterMethods
{
    public class CollectConnectionStatistics : CollectStatisticBase, IMasterMethod
    {
        public Task Do(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters, IList<IRpcClient> clients)
        {
            Log.Information($"Start to collect connections statistics...");

            CollectStatistics(stepParameters, pluginParameters, clients, ConnectionStatEventerCallback);
            return Task.CompletedTask;
        }
    }
}
