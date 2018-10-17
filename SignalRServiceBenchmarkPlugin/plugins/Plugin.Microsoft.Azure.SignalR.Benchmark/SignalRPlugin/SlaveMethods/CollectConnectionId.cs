using Common;
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
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionStore}.{type}", out IList<HubConnection> connections, (obj) => (IList<HubConnection>)obj);

                // Get connection Ids from app server
                var connectionIds = GetConnectionIds(connections);
            }
            catch (Exception ex)
            {
                var message = $"Fail to echo: {ex}";
                Log.Error(message);
                throw new Exception(message);
            }
        }

        private async Task<IList<string>> GetConnectionIds(IList<HubConnection> connections)
        {
            var connectionIds = new List<string>();

            // Set callback
            foreach (var connection in connections)
            {
                connection.On(SignalRConstants.GetConnectionIdCallback, (IDictionary<string, object> data) =>
                {
                    data.TryGetTypedValue(SignalRConstants.ConnectionId, out string connectionId, Convert.ToString);
                    connectionIds.Add(connectionId);
                });
            }

            // Query connection Id
            foreach (var connection in connections) await connection.SendAsync(SignalRConstants.GetConnectionIdCallback);

            return connectionIds;
        }


    }
}
