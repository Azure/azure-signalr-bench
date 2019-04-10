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
    public class StartConnection : IMasterMethod
    {
        public Task Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters, IList<IRpcClient> clients)
        {
            Log.Information($"Start connections...");

            // Get parameters
            stepParameters.TryGetTypedValue(SignalRConstants.ConcurrentConnection, out int concurrentConnection, Convert.ToInt32);

            if (concurrentConnection < clients.Count)
            {
                concurrentConnection = clients.Count;
                var message = $"Concurrent connection {concurrentConnection} should NOT be less than the number of slaves {clients.Count}, we force it to be {clients.Count}";
                Log.Warning(message);
            }

            var packages = clients.Select((client, i) =>
            {
                int currentConcurrentConnection = Util.SplitNumber(concurrentConnection, i, clients.Count);
                var data = new Dictionary<string, object>(stepParameters);
                data[SignalRConstants.ConcurrentConnection] = currentConcurrentConnection;
                return new { Client = client, Data = data };
            });

            var results = from package in packages select package.Client.QueryAsync(package.Data);

            var task = Task.WhenAll(results);
            // we wait until the default timeout reached
            return Util.TimeoutCheckedTask(task, SignalRConstants.MillisecondsToWait, nameof(StartConnection));
        }
    }
}
