using Plugin.Base;
using Rpc.Service;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    public class SignalRBenchmarkPlugin : IPugin
    {
        // TODO: not finish
        public async Task HandleMasterStep(MasterStep step, IList<IRpcClient> rpcClients)
        {
            var configuration = "\n";
            foreach(var entry in step.Parameters)
            {
                configuration += $"  {entry.Key}: {entry.Value}\n";
            }
            Log.Information($"Handle step...\nConfiguration: {configuration}");
        }
    }
}
