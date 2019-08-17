using Common;
using Plugin.Base;
using Rpc.Service;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.MasterMethods
{
    public class SendToClient : IMasterMethod
    {
        public Task Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters, IList<IRpcClient> clients)
        {
            Log.Information($"{GetType().Name}...");

            // Get parameters
            stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
            stepParameters.TryGetTypedValue(SignalRConstants.ConnectionTotal, out int connectionTotal, Convert.ToInt32);
            stepParameters.TryGetTypedValue(SignalRConstants.Duration, out long duration, Convert.ToInt64);
            pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionIdStore}.{type}",
                out IList<string> connectionIds, obj => (IList<string>) obj);

            // Shuffle connection Ids
            connectionIds.Shuffle();

            // Prepare parameters
            var packages = clients.Select((client, i) =>
            {
                (int beg, int end) = Util.GetConnectionRange(connectionTotal, i, clients.Count);
                var data = new Dictionary<string, object>(stepParameters);
                data[SignalRConstants.ConnectionIdStore] = string.Join(' ', connectionIds.ToList().GetRange(beg, end - beg));
                return new { Client = client, Data = data };
            });

            // Process on clients
            var task = Task.WhenAll(from package in packages select package.Client.QueryAsync(package.Data));
            return Util.TimeoutCheckedTask(task, duration * 2, GetType().Name);
        }
    }
}
