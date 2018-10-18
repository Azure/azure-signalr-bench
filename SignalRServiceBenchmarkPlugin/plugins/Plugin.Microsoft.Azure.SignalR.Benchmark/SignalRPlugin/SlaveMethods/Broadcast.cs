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
    public class Broadcast : BaseContinuousSendMethod, ISlaveMethod
    {
        private StatisticsCollector _statistics;
        public async Task<IDictionary<string, object>> Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Broadcast...");

                // Get parameters
                stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
                stepParameters.TryGetTypedValue(SignalRConstants.RemainderBegin, out int remainderBegin, Convert.ToInt32);
                stepParameters.TryGetTypedValue(SignalRConstants.RemainderEnd, out int remainderEnd, Convert.ToInt32);
                stepParameters.TryGetTypedValue(SignalRConstants.Modulo, out int modulo, Convert.ToInt32);
                stepParameters.TryGetTypedValue(SignalRConstants.Duration, out double duration, Convert.ToDouble);
                stepParameters.TryGetTypedValue(SignalRConstants.Interval, out double interval, Convert.ToDouble);
                stepParameters.TryGetTypedValue(SignalRConstants.MessageSize, out int messageSize, Convert.ToInt32);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionStore}.{type}", out IList<HubConnection> connections, (obj) => (IList<HubConnection>)obj);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionOffset}.{type}", out int offset, Convert.ToInt32);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.StatisticsStore}.{type}", out _statistics, obj => (StatisticsCollector) obj);

                // Set callback
                SetCallback(connections);
                
                // Generate necessary data
                var data = new Dictionary<string, object>
                {
                    { SignalRConstants.MessageBlob, new byte[messageSize] } // message payload
                };

                // Send messages
                await Task.WhenAll(from i in Enumerable.Range(0, connections.Count)
                                    where (i + offset) % modulo >= remainderBegin && (i + offset) % modulo < remainderEnd
                                    select ContinuousSend(connections[i], data, SendBroadcast,
                                            TimeSpan.FromMilliseconds(duration), TimeSpan.FromMilliseconds(interval), 
                                            TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(interval)));

                return null;
            }
            catch (Exception ex)
            {
                var message = $"Fail to broadcast: {ex}";
                Log.Error(message);
                throw new Exception(message);
            }
        }

        private void SetCallback(IList<HubConnection> connections)
        {
            foreach (var connection in connections)
            {
                connection.On(SignalRConstants.BroadcastCallbackName, (IDictionary<string, object> data) =>
                {
                    var receiveTimestamp = Util.Timestamp();
                    data.TryGetTypedValue(SignalRConstants.Timestamp, out long sendTimestamp, Convert.ToInt64);
                    var latency = receiveTimestamp - sendTimestamp;
                    _statistics.RecordLatency(latency);
                });
            }
        }

        private async Task SendBroadcast(HubConnection connection, IDictionary<string, object> data)
        {
            try
            {
                // Extract data
                data.TryGetValue(SignalRConstants.MessageBlob, out var messageBlob);

                // Prepare payload
                var payload = new Dictionary<string, object>
                {
                    { SignalRConstants.MessageBlob, messageBlob },
                    { SignalRConstants.Timestamp, Util.Timestamp() }
                };

                // Send message
                await connection.SendAsync(SignalRConstants.BroadcastCallbackName, payload);

                // Update statistics
                _statistics.IncreaseSentMessage();
            }
            catch (Exception ex)
            {
                var message = $"Error in broadcast: {ex}";
                Log.Error(message);
                throw new Exception(message);
            }
        }
    }
}
