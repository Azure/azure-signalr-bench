using Plugin.Base;
using Rpc.Service;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.MasterMethods
{
    public class InitStatisticsCollector : InitStatisticsCollectorBase, IMasterMethod
    {
        public Task Do(IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters, IList<IRpcClient> clients)
        {
            Log.Information($"Start statistic collector...");
            return Run(stepParameters, pluginParameters, clients);
        }
    }
}
