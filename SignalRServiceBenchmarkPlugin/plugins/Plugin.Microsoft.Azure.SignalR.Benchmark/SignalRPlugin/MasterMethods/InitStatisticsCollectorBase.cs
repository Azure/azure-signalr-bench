using Common;
using Rpc.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.MasterMethods
{
    public class InitStatisticsCollectorBase
    {
        protected Task Run(IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters, IList<IRpcClient> clients)
        {
            // Get parameters
            stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
            stepParameters.TryGetTypedValue(SignalRConstants.LatencyStep, out long latencyStep, Convert.ToInt64);
            stepParameters.TryGetTypedValue(SignalRConstants.LatencyMax, out long latencyMax, Convert.ToInt64);

            stepParameters[$"{SignalRConstants.LatencyStep}.{type}"] = latencyStep;
            stepParameters[$"{SignalRConstants.LatencyMax}.{type}"] = latencyMax;

            pluginParameters[$"{SignalRConstants.LatencyStep}.{type}"] = latencyStep;
            pluginParameters[$"{SignalRConstants.LatencyMax}.{type}"] = latencyMax;

            return Task.WhenAll(from client in clients
                                select client.QueryAsync(stepParameters));
        }
    }
}
