using Plugin.Base;
using Rpc.Service;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.MasterMethods
{
    public class CollectConnectionId : IMasterMethod
    {
        public async Task Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters, IList<IRpcClient> clients)
        {
            Log.Information($"Collect connection ID...");

            // Process on clients
            var results = await Task.WhenAll(from client in clients select client.QueryAsync(stepParameters));

            // Collect all connection Ids
            var allConnectionIds = results.SelectMany(dict => dict).ToDictionary(entry => entry.Key, entry => entry.Value);
        }
    }
}
