using Plugin.Base;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.AgentMethods
{
    public class CollectConnectionStatistics : CollectStatisticBase, IAgentMethod
    {
        public Task<IDictionary<string, object>> Do(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters)
        {
            Log.Information($"Collect statistics...");
            return Run(stepParameters, pluginParameters);
        }
    }
}
