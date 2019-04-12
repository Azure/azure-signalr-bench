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

                // Init the connection Id list
                var connectionIdList = new List<string>();
                for (var m = 0; m < connections.Count; m++)
                {
                    connectionIdList.Add("");
                }

                var connIdStoreKey = $"{SignalRConstants.ConnectionIdStore}.{type}";
                /*
                if (pluginParameters.TryGetValue(connIdStoreKey, out object v))
                {
                    // check whether need re-get connection Ids
                    var allConnIds = (Dictionary<string, object>)v;
                    var existingConnIdList = (string[])allConnIds[SignalRConstants.ConnectionId];
                    var hasMissingConnectionId = false;
                    foreach (var connId in existingConnIdList)
                    {
                        if (String.IsNullOrEmpty(connId))
                        {
                            hasMissingConnectionId = true;
                        }
                    }

                    if (!hasMissingConnectionId)
                    {
                        // all connection Ids are ready
                        Log.Information("No missing connection Ids, skip try-re-get connection Ids");
                        return allConnIds;
                    }
                    else
                    {
                        // prepare re-get missing connection Ids
                        for (var m = 0; m < existingConnIdList.Length; m++)
                        {
                            connectionIdList[m] = existingConnIdList[m];
                        }
                    }
                }
                */
                // get connection Ids
                int concurrentConnection =
                    SignalRUtils.FetchConcurrentConnectionCountFromContext(
                    pluginParameters,
                    type,
                    connections.Count);
                await SendRequestToGetConnectionIds(connections, concurrentConnection, connectionIdList);

                var connectionIdDic = new Dictionary<string, object> { { SignalRConstants.ConnectionId, connectionIdList.ToArray() } };
                pluginParameters[connIdStoreKey] = connectionIdDic;
                return connectionIdDic;
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
            IList<string> connectionIdList)
        {
            var packages = (from i in Enumerable.Range(0, connections.Count())
                            select (Connection: connections[i], connectionIdList, i)).ToList();
            await Util.LowPressBatchProcess(
                packages,
                SaveConnectionId,
                concurrentSend,
                SignalRConstants.BatchProcessDefaultWait);
        }

        private async Task SaveConnectionId(
            (IHubConnectionAdapter connection,
            IList<string> connectionIdList,
            int index) package)
        {
            try
            {
                if (package.connection.GetStat() != SignalREnums.ConnectionInternalStat.Active)
                    return;
                package.connection.On(
                        SignalRConstants.ConnectionIdCallback,
                        (string connectionId) => package.connectionIdList[package.index] = connectionId);
                await package.connection.SendAsync(SignalRConstants.ConnectionIdCallback);
            }
            catch (Exception ex)
            {
                Log.Warning($"Fail to get connection Id because of {ex.Message}");
            }
        }
    }
}
