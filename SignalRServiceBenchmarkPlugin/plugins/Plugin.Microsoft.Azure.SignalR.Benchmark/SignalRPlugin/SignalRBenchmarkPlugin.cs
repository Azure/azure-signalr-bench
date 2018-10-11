using Plugin.Base;
using Rpc.Service;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Common;
using System.Linq;
using Newtonsoft.Json;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    // Sample plugin: SignalR benchmark
    public class SignalRBenchmarkPlugin : IPlugin
    {
        private MasterStepActionBroker _masterActionBroker = new MasterStepActionBroker();
            
        public async Task HandleMasterStep(MasterStep step, IList<IRpcClient> clients)
        {
            // Show step configuration
            ShowConfiguration(step);

            // Send to slaves
            await SendToSlaves(step, clients);
        }

        public async Task HandleSlaveStep(IDictionary<string, object> parameters)
        {
            // Send to master
            await SendToSlaves(parameters);
        }

        private Task SendToSlaves(IDictionary<string, object> parameters)
        {
            var method = parameters[Constants.Method];
            var type = parameters[Constants.Type];

            switch(method)
            {
                case "CreateConnection":
                    // TODO: reflection
                    return _masterActionBroker.CreateConnection(parameters);
                default:
                    break;
            }
            return Task.CompletedTask;
        }

        private Task SendToSlaves(MasterStep step, IList<IRpcClient> clients)
        {
            var method = step.GetMethod();
            var parameters = step.Parameters;
            switch (method)
            {
                case "CreateConnection":
                    // TODO: reflection
                    return _masterActionBroker.CreateConnection(parameters, clients);
                default:
                    break;
            }
            return Task.CompletedTask;
        }

        private void ShowConfiguration(MasterStep step)
        {
            var configuration = "\n";
            foreach (var entry in step.Parameters)
            {
                configuration += $"  {entry.Key}: {entry.Value}\n";
            }
            Log.Information($"Handle step...\nConfiguration: {configuration}");
        }
    }
}
