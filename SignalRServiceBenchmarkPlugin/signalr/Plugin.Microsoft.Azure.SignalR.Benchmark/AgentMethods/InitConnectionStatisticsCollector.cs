using Plugin.Base;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.AgentMethods
{
    public class InitConnectionStatisticsCollector : InitStatisticsCollectorBase, IAgentMethod
    {
        public Task<IDictionary<string, object>> Do(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters)
        {
            Log.Information($"Init connection statistic collector...");
            return Run(stepParameters, pluginParameters, RegisterConnectionStatistics);
        }
    }
}
