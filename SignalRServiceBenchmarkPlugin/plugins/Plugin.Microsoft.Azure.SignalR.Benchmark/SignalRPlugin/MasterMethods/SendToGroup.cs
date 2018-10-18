using Microsoft.AspNetCore.SignalR.Client;
using Plugin.Base;
using Rpc.Service;
using System.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Common;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.MasterMethods
{
    public class SendToGroup : IMasterMethod
    {
        public Task Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters, IList<IRpcClient> clients)
        {
            Log.Information($"Send to group...");

            // Get parameters
            stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
            stepParameters.TryGetTypedValue(SignalRConstants.GroupCount, out int groupCount, Convert.ToInt32);
            stepParameters.TryGetTypedValue(SignalRConstants.Duration, out long duration, Convert.ToInt64);
            stepParameters.TryGetTypedValue(SignalRConstants.Interval, out long interval, Convert.ToInt64);
            stepParameters.TryGetTypedValue(SignalRConstants.MessageSize, out int messageSize, Convert.ToInt32);

            // Get context
            pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionIdStore}.{type}", out IList<string> connectionIds, obj => (IList<string>) obj);

            // Prepare parameters
            var packages = clients.Select((client, i) =>
            {
                var data = new Dictionary<string, object>
                {
                };
                // Add method and type
                PluginUtils.AddMethodAndType(data, stepParameters);
                return new { Client = client, Data = data };
            });

            // Process on clients
            return Task.WhenAll(from package in packages select package.Client.QueryAsync(package.Data));
        }
    }
}
