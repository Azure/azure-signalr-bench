using Common;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Plugin.Base;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public class CollectConnectionId : ISlaveMethod
    {
        public async Task<IDictionary<string, object>> Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Collect connection ID...");

                // Get parameters
                stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionStore}.{type}",
                    out IList<IHubConnectionAdapter> connections, (obj) => (IList<IHubConnectionAdapter>)obj);

                var connectionIdList = new List<string>();
                SetConnectionIdCallback(connections, connectionIdList);

                // Get connection Ids from app server
                // var connectionIds = await GetConnectionIds(connections);
                await Util.BatchProcess(connections, ServerSendConnectionIdBack, 20);
                // Wait for all connection Ids are received
                var i = 0;
                var maxTry = 5;
                while (connectionIdList.Count < connections.Count && i < maxTry)
                {
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

        public void SetConnectionIdCallback(
            IList<IHubConnectionAdapter> connections, IList<string> connectionIdList)
        {
            foreach (var connection in connections)
            {
                connection.On(SignalRConstants.ConnectionId, (string connectionId) => connectionIdList.Add(connectionId));
            }
        }

        private async Task<IDictionary<string, object>> GetConnectionIds(IList<IHubConnectionAdapter> connections)
        {

            // Query connection Id
            var connectionIds = await Util.BatchProcess(connections, CollectConnectionIdFromServer<string>, 20);

            return new Dictionary<string, object> { { SignalRConstants.ConnectionId, connectionIds } };
        }

        private Task ServerSendConnectionIdBack(IHubConnectionAdapter connection)
        {
            return connection.SendAsync(SignalRConstants.ConnectionIdCallback);
        }

        private Task<T> CollectConnectionIdFromServer<T>(IHubConnectionAdapter connection)
        {
            var i = 0;
            var maxTry = 3;
            Task<T> result = null;
            while (i < maxTry)
            {
                try
                {
                    result = connection.InvokeAsync<T>(SignalRConstants.GetConnectionIdCallback);
                }
                catch (HubException ex)
                {
                    Log.Error($"Encounter {ex.Message}");
                    if (i < maxTry)
                    {
                        Log.Information($"Try to get connection Id again");
                    }
                }
                i++;
            }
            return result;
        }
    }
}
