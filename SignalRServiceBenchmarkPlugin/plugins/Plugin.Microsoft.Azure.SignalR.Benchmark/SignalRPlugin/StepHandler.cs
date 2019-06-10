using Plugin.Base;
using Rpc.Service;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    public class StepHandler
    {
        ISignalRPlugin _plugin;

        public StepHandler(IPlugin plugin)
        {
            _plugin = (ISignalRPlugin)plugin;
        }

        public async Task HandleStep(
            MasterStep step,
            IList<IRpcClient> clients,
            bool debug)
        {
            // Show step configuration
            if (debug)
            {
                PluginUtils.ShowConfiguration(step.Parameters);
            }

            // Send to slaves
            await SendToSlaves(step, clients);
        }

        private Task SendToSlaves(MasterStep step, IList<IRpcClient> clients)
        {
            try
            {
                var method = step.GetMethod();
                var parameters = step.Parameters;

                // Create instance
                IMasterMethod methodInstance = _plugin.CreateMasterMethodInstance(method);

                // Do action
                return methodInstance.Do(parameters, _plugin.PluginMasterParameters, clients);
            }
            catch (Exception ex)
            {
                var message = $"Fail to handle step: {ex}";
                Log.Error(message);
                throw;
            }
            
        }
    }
}
