using Common;
using Plugin.Base;
using Rpc.Service;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.MasterMethod
{
    public class CreateConnection : IMasterMethod
    {
        public Task Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters, IList<IRpcClient> clients)
        {
            var success = true;

            success = stepParameters.TryGetTypedValue(SignalRConstants.ConnectionTotal, out int connectionTotal, Convert.ToInt32);
            PluginUtils.HandleGetValueResult(success, SignalRConstants.ConnectionTotal);

            success = stepParameters.TryGetTypedValue(SignalRConstants.HubUrl, out string hubUrl, Convert.ToString);
            PluginUtils.HandleGetValueResult(success, SignalRConstants.HubUrl);

            success = stepParameters.TryGetTypedValue(SignalRConstants.TransportType, out string transportType, Convert.ToString);
            PluginUtils.HandleGetValueResult(success, SignalRConstants.TransportType);

            success = stepParameters.TryGetTypedValue(SignalRConstants.HubProtocol, out string hubProtocol, Convert.ToString);
            PluginUtils.HandleGetValueResult(success, SignalRConstants.HubProtocol);

            var packages = clients.Select((client, i) => {
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

            var results = from package in packages select package.Client.QueryAsync(package.Data);
            return Task.WhenAll(results);
        }
    }
}
