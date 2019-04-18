using Plugin.Base;
using Rpc.Service;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.MasterMethods
{
    public class RepairConnections : IMasterMethod
    {
        public Task Do(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters,
            IList<IRpcClient> clients)
        {
            Log.Information($"{GetType().Name}...");
            return Task.WhenAll(from client in clients select client.QueryAsync(stepParameters));
        }
    }
}
