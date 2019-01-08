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
    public class SendToClient : IMasterMethod
    {
        public Task Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters, IList<IRpcClient> clients)
        {
            Log.Information($"Send to client...");

            // Get parameters
            stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
            stepParameters.TryGetTypedValue(SignalRConstants.RemainderBegin, out int remainderBegin, Convert.ToInt32);
            stepParameters.TryGetTypedValue(SignalRConstants.RemainderEnd, out int remainderEnd, Convert.ToInt32);
            stepParameters.TryGetTypedValue(SignalRConstants.Modulo, out int modulo, Convert.ToInt32);
            stepParameters.TryGetTypedValue(SignalRConstants.ConnectionTotal, out int connectionTotal, Convert.ToInt32);
            stepParameters.TryGetTypedValue(SignalRConstants.Duration, out long duration, Convert.ToInt64);
            stepParameters.TryGetTypedValue(SignalRConstants.Interval, out long interval, Convert.ToInt64);
            stepParameters.TryGetTypedValue(SignalRConstants.MessageSize, out int messageSize, Convert.ToInt32);
            pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionIdStore}.{type}", out IList<string> connectionIds, obj => (IList<string>) obj);

            // Shuffle connection Ids
            connectionIds.Shuffle();

            // Prepare parameters
            var packages = clients.Select((client, i) =>
            {
                (int beg, int end) = Util.GetConnectionRange(connectionTotal, i, clients.Count);
                var data = new Dictionary<string, object>
                {
                    { SignalRConstants.RemainderBegin, remainderBegin},
                    { SignalRConstants.RemainderEnd, remainderEnd},
                    { SignalRConstants.Modulo, modulo},
                    { SignalRConstants.Duration, duration},
                    { SignalRConstants.Interval, interval},
                    { SignalRConstants.MessageSize, messageSize},
                    { SignalRConstants.ConnectionIdStore, string.Join(' ', connectionIds.ToList().GetRange(beg, end - beg))}
                };
                // Add method and type
                PluginUtils.AddMethodAndType(data, stepParameters);
                return new { Client = client, Data = data };
            });

            // Process on clients
            var task = Task.WhenAll(from package in packages select package.Client.QueryAsync(package.Data));
            return Util.TimeoutCheckedTask(task, duration * 2, nameof(SendToClient));
        }
    }
}
