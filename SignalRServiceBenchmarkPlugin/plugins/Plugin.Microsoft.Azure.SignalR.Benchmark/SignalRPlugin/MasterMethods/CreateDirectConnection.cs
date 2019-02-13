using Common;
using Plugin.Base;
using Rpc.Service;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.MasterMethods
{
    public class CreateDirectConnection : IMasterMethod
    {
        public Task Do(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters,
            IList<IRpcClient> clients)
        {
            Log.Information($"Create direct connections...");

            var ret = SignalRUtils.MasterCreateConnection(stepParameters, pluginParameters, clients);
            SignalRUtils.MarkConnectionType(stepParameters, pluginParameters, SignalREnums.ClientType.DirectConnect);
            return ret;
        }
    }
}
