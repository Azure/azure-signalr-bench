using Common;
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
    public class CreateConnection : IMasterMethod
    {
        public Task Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters, IList<IRpcClient> clients)
        {
            Log.Information($"Create connections...");

            // Get parameters
            stepParameters.TryGetTypedValue(SignalRConstants.ConnectionTotal, out int connectionTotal, Convert.ToInt32);
            stepParameters.TryGetTypedValue(SignalRConstants.HubUrl, out string hubUrl, Convert.ToString);
            stepParameters.TryGetTypedValue(SignalRConstants.TransportType, out string transportType, Convert.ToString);
            stepParameters.TryGetTypedValue(SignalRConstants.HubProtocol, out string hubProtocol, Convert.ToString);

            // Prepare configuration for each clients
            var packages = clients.Select((client, i) => 
            {
                (int beg, int end) = Util.GetConnectionRange(connectionTotal, i, clients.Count);
                var data = new Dictionary<string, object>
                {
                    { SignalRConstants.ConnectionBegin, beg },
                    { SignalRConstants.ConnectionEnd, end },
                    { SignalRConstants.HubUrl, hubUrl },
                    { SignalRConstants.TransportType, transportType },
                    { SignalRConstants.HubProtocol, hubProtocol }
                };
                // Add method and type
                PluginUtils.AddMethodAndType(data, stepParameters);
                return new { Client = client, Data = data};
            });

            // Process on clients
            var results = from package in packages select package.Client.QueryAsync(package.Data);
            return Task.WhenAll(results);
        }
    }
}
