using Common;
using Plugin.Base;
using Rpc.Service;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.MasterMethods
{
    public class StopCollector : IMasterMethod
    {
        public Task Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters, IList<IRpcClient> clients)
        {
            ICollector collector = null;

            try
            {
                Log.Information($"Stop collecting...");

                // Get parameters
                stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.StopCollector}.{type}", out collector, obj => (ICollector)obj);
            }
            finally
            {
                // Stop and dispose timer
                if (collector != null)
                {
                    collector.StopCollector();
                }
            }

            return Task.CompletedTask;
        }
    }
}
