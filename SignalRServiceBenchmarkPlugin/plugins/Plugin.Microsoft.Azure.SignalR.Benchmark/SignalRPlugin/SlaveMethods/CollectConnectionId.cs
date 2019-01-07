using Common;
using Plugin.Base;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
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
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionSuccessFlag}.{type}",
                    out List<SignalREnums.ConnectionState> connectionsSuccessFlag,
                    (obj) => (List<SignalREnums.ConnectionState>)obj);

                // Init the connection Id list
                var connectionIdList = new List<string>();
                for (var m = 0; m < connections.Count; m++)
                {
                    connectionIdList.Add("");
                }

                int concurrentConnection = 100;
                if (pluginParameters.TryGetValue(SignalRConstants.ConcurrentConnection, out object v))
                {
                    pluginParameters.TryGetTypedValue(SignalRConstants.ConcurrentConnection, out int value, Convert.ToInt32);
                    concurrentConnection = value;
                }

                await SendRequestToGetConnectionIds(connections, concurrentConnection, connectionIdList, connectionsSuccessFlag);
                var failures = FailedConnectionId(connectionIdList);
                if (failures > 0)
                {
                    var failureRate = (float)(failures * 100 / connections.Count);
                    if (failureRate > 1.0)
                    {
                        Log.Error($"Too many failures, and we will abandon the reconnection");
                    }
                    else
                    {
                        // reconnect if connection drops
                        var packages = (from i in Enumerable.Range(0, connections.Count())
                                        select (Connection: connections[i], LocalIndex: i,
                                        ConnectionsSuccessFlag: connectionsSuccessFlag,
                                        NormalState: SignalREnums.ConnectionState.Success,
                                        AbnormalState: SignalREnums.ConnectionState.Fail)).ToList();
                        await Task.WhenAll(Util.BatchProcess(packages, SignalRUtils.StartConnect, concurrentConnection));
                        await SendRequestToGetConnectionIds(connections, concurrentConnection, connectionIdList, connectionsSuccessFlag);
                    }
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

        private int FailedConnectionId(IList<string> connectionIdList)
        {
            var failedConnection = 0;
            for (var i = 0; i < connectionIdList.Count; i++)
            {
                if (String.IsNullOrEmpty(connectionIdList[i]))
                {
                    failedConnection++;
                }
            }
            return failedConnection;
        }

        private async Task SendRequestToGetConnectionIds(
            IList<IHubConnectionAdapter> connections,
            int concurrentSend,
            IList<string> connectionIdList,
            List<SignalREnums.ConnectionState> connectionsSuccessFlag)
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
                            if (String.IsNullOrEmpty(connectionIdList[index]))
                            {
                                await SaveConnectionId(connections[index], connectionIdList, connectionsSuccessFlag, index);
                            }
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

        private async Task SaveConnectionId(
            IHubConnectionAdapter connection,
            IList<string> connectionIdList,
            List<SignalREnums.ConnectionState> connectionsSuccessFlag,
            int index)
        {
            try
            {
                if (String.IsNullOrEmpty(connectionIdList[index]))
                {
                    connection.On(SignalRConstants.ConnectionIdCallback, (string connectionId) => connectionIdList[index] = connectionId);
                    await connection.SendAsync(SignalRConstants.ConnectionIdCallback);
                }
            }
            catch (System.InvalidOperationException ex)
            {
                Log.Warning($"Fail to get connection Id because of {ex.Message}");
                connectionsSuccessFlag[index] = SignalREnums.ConnectionState.Fail;
            }
            //var t = connection.InvokeAsync<string>(SignalRConstants.GetConnectionIdCallback);
            //await t;
            //connectionIdList[index] = t.Result;
        }
    }
}
