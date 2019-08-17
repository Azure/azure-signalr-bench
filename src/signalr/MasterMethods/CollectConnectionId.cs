using Common;
using Newtonsoft.Json.Linq;
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
    public class CollectConnectionId : IMasterMethod
    {
        public async Task Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters, IList<IRpcClient> clients)
        {
            Log.Information($"Collect connection ID...");

            // Get parameters
            stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);

            var allTasks = from client in clients select client.QueryAsync(stepParameters);
            // Process on clients
            try
            {
                var results = await TimedoutTask.TimeoutAfter(
                    Task.WhenAll(allTasks),
                    TimeSpan.FromMilliseconds(SignalRConstants.MillisecondsToWait));
                // Collect all connection Ids
                var allConnectionIds = new List<string>();
                foreach (var result in results)
                {
                    result.TryGetTypedValue(
                        SignalRConstants.ConnectionId,
                        out List<string> connectionIds,
                        obj => ((JArray)obj).ToObject<List<string>>());
                    allConnectionIds.AddRange(connectionIds);
                }

                // Store connection Ids
                pluginParameters[$"{SignalRConstants.ConnectionIdStore}.{type}"] = allConnectionIds;
            }
            catch (Exception e)
            {
                Log.Error($"Fail to collect connection Id for {e.Message}");
            }
        }
    }
}
