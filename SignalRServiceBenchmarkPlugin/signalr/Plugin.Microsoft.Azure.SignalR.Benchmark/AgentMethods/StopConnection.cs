using Common;
using Plugin.Base;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.AgentMethods
{
    public class StopConnection: IAgentMethod
    {
        public async Task<IDictionary<string, object>> Do(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Stop connections...");

                // Get parameters
                stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
                
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionStore}.{type}",
                    out IList<IHubConnectionAdapter> connections, (obj) => (IList<IHubConnectionAdapter>) obj);
                // Stop the possible scanner
                StopRapirConnectionScanner(stepParameters, pluginParameters);
                // Stop connections
                await Task.WhenAll(from connection in connections
                                   select connection.StopAsync());
                await SignalRUtils.StopNegotiationServer(stepParameters, pluginParameters);
                await SignalRUtils.StopInternalAppServer(stepParameters, pluginParameters);
                return null;
            }
            catch (Exception ex)
            {
                var message = $"Fail to stop connections: {ex}";
                Log.Error(message);
                throw;
            }
        }

        private void StopRapirConnectionScanner(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters)
        {
            stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
            if (pluginParameters.TryGetValue($"{SignalRConstants.RepairConnectionCTS}.{type}", out _))
            {
                pluginParameters.TryGetTypedValue($"{SignalRConstants.RepairConnectionCTS}.{type}",
                    out CancellationTokenSource cts, (obj) => (CancellationTokenSource) obj);
                cts.Cancel();
                cts.Dispose();
            }
        }
    }
}
