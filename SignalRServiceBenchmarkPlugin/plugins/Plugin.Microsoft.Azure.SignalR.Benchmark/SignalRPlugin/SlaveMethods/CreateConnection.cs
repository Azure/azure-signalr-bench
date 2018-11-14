using Common;
using Microsoft.AspNetCore.SignalR.Client;
using Plugin.Base;
using Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods.Statistics;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public class CreateConnection : ISlaveMethod
    {
        public async Task<IDictionary<string, object>> Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Create connections...");

                // Get parameters
                stepParameters.TryGetTypedValue(SignalRConstants.HubUrls, out string urls, Convert.ToString);
                stepParameters.TryGetTypedValue(SignalRConstants.HubProtocol, out string protocol, Convert.ToString);
                stepParameters.TryGetTypedValue(SignalRConstants.TransportType, out string transportType, Convert.ToString);
                stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
                stepParameters.TryGetTypedValue(SignalRConstants.ConnectionIndex, out string connectionIndexString, Convert.ToString);

                var connectionIndex = connectionIndexString.Split(',').Select(ind => Convert.ToInt32(ind)).ToList();

                // Create Connections
                var connections = SignalRUtils.CreateConnections(connectionIndex, urls, transportType, protocol, SignalRConstants.ConnectionCloseTimeout);

                // Setup connection success flag
                var connectionsSuccessFlag = Enumerable.Repeat(SignalREnums.ConnectionState.Init, connections.Count()).ToList();

                // Setup connection drop handler
                SignalRUtils.SetConnectionOnClose(connections, connectionsSuccessFlag);

                // Prepare plugin parameters
                pluginParameters[$"{SignalRConstants.ConnectionStore}.{type}"] = connections;
                pluginParameters[$"{SignalRConstants.ConnectionIndex}.{type}"] = connectionIndex;
                pluginParameters[$"{SignalRConstants.ConnectionSuccessFlag}.{type}"] = connectionsSuccessFlag;
                pluginParameters[$"{SignalRConstants.RegisteredCallbacks}.{type}"] = new List<Action<IList<HubConnection>, StatisticsCollector, string>>();

                return null;
            }
            catch (Exception ex)
            {
                var message = $"Fail to create connections: {ex}";
                Log.Error(message);
                throw;
            }
        }
    }
}
