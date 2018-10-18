using Common;
using Microsoft.AspNetCore.SignalR.Client;
using Plugin.Base;
using Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods.Statistics;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public class SendToGroup : BaseContinuousSendMethod, ISlaveMethod
    {
        private StatisticsCollector _statisticsCollector;

        public async Task<IDictionary<string, object>> Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Send to group...");

                // Get parameters
                
                // Get context
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionStore}.{type}", out IList<HubConnection> connections, (obj) => (IList<HubConnection>)obj);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionOffset}.{type}", out int offset, Convert.ToInt32);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.StatisticsStore}.{type}", out _statisticsCollector, obj => (StatisticsCollector) obj);

                // Set callback
                SetCallback(connections);

                // Generate necessary data
                var messageBlob = new byte[messageSize];

                

                // Send messages
                

                return null;
            }
            catch (Exception ex)
            {
                var message = $"Fail to send to group: {ex}";
                Log.Error(message);
                throw;
            }
        }

        private void SetCallback(IList<HubConnection> connections)
        {
            foreach (var connection in connections)
            {
                connection.On(SignalRConstants.SendToClientCallbackName, (IDictionary<string, object> data) =>
                {
                    var receiveTimestamp = Util.Timestamp();
                    data.TryGetTypedValue(SignalRConstants.Timestamp, out long sendTimestamp, Convert.ToInt64);
                    var latency = receiveTimestamp - sendTimestamp;
                    _statisticsCollector.RecordLatency(latency);
                });
            }
        }

        private async Task SendClient(HubConnection connection, IDictionary<string, object> data)
        {
            try
            {

                // Generate payload
                var payload = new Dictionary<string, object>
                {
                };

                // Send message
                await connection.SendAsync(SignalRConstants., payload);

                // Update statistics
                _statisticsCollector.IncreaseSentMessage();
            }
            catch (Exception ex)
            {
                var message = $"Error in send to group: {ex}";
                Log.Error(message);
                throw;
            }
        }
    }
}
