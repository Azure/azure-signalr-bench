using Plugin.Base;
using Rpc.Service;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.MasterMethods
{
    public class RegisterCallbackOnConnected : IMasterMethod
    {
        public Task Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters, IList<IRpcClient> clients)
        {
            Log.Information($"Register callback for notification OnConnected...");

            // Process on clients
            return Task.WhenAll(from client in clients select client.QueryAsync(stepParameters));
        }
    }
}
