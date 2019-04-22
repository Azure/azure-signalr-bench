using Plugin.Base;
using Rpc.Service;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.MasterMethods
{
    public class RestSendToGroup : SimpleScenarioBase, IMasterMethod
    {
        public Task Do(
                    IDictionary<string, object> stepParameters,
                    IDictionary<string, object> pluginParameters,
                    IList<IRpcClient> clients)
        {
            return Run(stepParameters, pluginParameters, clients);
        }
    }
}
