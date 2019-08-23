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
    public class DisposeConnection: IMasterMethod
    {
        public Task Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters, IList<IRpcClient> clients)
        {
            Log.Information($"Dispose connections...");

            var data = new Dictionary<string, object>();

            SignalRUtils.AddMethodAndType(data, stepParameters);

            return Task.WhenAll(from client in clients
                                select client.QueryAsync(data));
        }
    }
}
