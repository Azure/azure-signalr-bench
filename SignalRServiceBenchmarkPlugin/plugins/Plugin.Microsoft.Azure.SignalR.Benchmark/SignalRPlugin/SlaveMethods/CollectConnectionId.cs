using Common;
using Plugin.Base;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public class CollectConnectionId : ISlaveMethod
    {
        public async Task<IDictionary<string, object>> Do(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Collect connection ID...");

                // Get parameters
                stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionStore}.{type}",
                    out IList<IHubConnectionAdapter> connections, (obj) => (IList<IHubConnectionAdapter>)obj);

                var connectionIdList = new List<string>();
                for (var m = 0; m < connections.Count; m++)
                {
                    connectionIdList.Add("");
                }

                int concurrent = 100;
                if (pluginParameters.TryGetValue(SignalRConstants.ConcurrentConnection, out object v))
                {
                    pluginParameters.TryGetTypedValue(SignalRConstants.ConcurrentConnection, out int value, Convert.ToInt32);
                    concurrent = value;
                }
                await SendRequestToGetConnectionIds(connections, concurrent, connectionIdList);
                // Wait for all connection Ids are received
                var i = 0;
                var maxTry = 5;
                while (connectionIdList.Count < connections.Count && i < maxTry)
                {
                    Log.Information($"see {connectionIdList.Count} but expect {connections.Count}");
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    i++;
                }
                if (i == maxTry && connectionIdList.Count < connections.Count)
                {
                    Log.Warning($"Only get {connectionIdList.Count} connection Ids but expect to see {connections.Count}");
                }
                return new Dictionary<string, object> { { SignalRConstants.ConnectionId, connectionIdList.ToArray() } };
            }
            catch (Exception ex)
            {
                var message = $"Fail to collect connection ID: {ex}";
                Log.Error(message);
                throw;
            }
        }

        private async Task SendRequestToGetConnectionIds(
            IList<IHubConnectionAdapter> connections,
            int concurrentSend,
            IList<string> connectionIdList)
        {
            var nextBatch = concurrentSend;
            var left = connections.Count;
            if (nextBatch <= left)
            {
                var tasks = new List<Task>(connections.Count);
                var i = 0;
                do
                {
                    for (var j = 0; j < nextBatch; j++)
                    {
                        var index = i + j;
                        tasks.Add(Task.Run(async () =>
                        {
                            await SaveConnectionId(connections[index], connectionIdList, index);
                        }));
                    }

                    await Task.Delay(TimeSpan.FromSeconds(1));
                    i += nextBatch;
                    left = left - nextBatch;
                    if (left < nextBatch)
                    {
                        nextBatch = left;
                    }
                } while (left > 0);
                await Task.WhenAll(tasks);
            }
        }

        private async Task SaveConnectionId(IHubConnectionAdapter connection, IList<string> connectionIdList, int index)
        {
            connection.On(SignalRConstants.ConnectionIdCallback, (string connectionId) => connectionIdList[index] = connectionId);
            await connection.SendAsync(SignalRConstants.ConnectionIdCallback);
            //var t = connection.InvokeAsync<string>(SignalRConstants.GetConnectionIdCallback);
            //await t;
            //connectionIdList[index] = t.Result;
        }
    }
}
