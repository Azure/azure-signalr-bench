using Microsoft.AspNetCore.SignalR.Client;
using Plugin.Base;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public class StopConnection: ISlaveMethod
    {
        public Task Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Stop connections...");
            
                // Get parameters
                PluginUtils.TryGetTypedValue(stepParameters, SignalRConstants.Type, out string type, Convert.ToString);
                PluginUtils.TryGetTypedValue(pluginParameters, SignalRConstants.ConnectionStore, out IList<HubConnection> connections, (obj) => (IList<HubConnection>) obj);

                // Stop connections
                return Task.WhenAll(from connection in connections
                                    select connection.StopAsync());
            }
            catch (Exception ex)
            {
                var message = $"Fail to stop connections: {ex}";
                Log.Error(message);
                throw new Exception(message);
            }
        }
    }
}
