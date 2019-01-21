using Common;
using Plugin.Base;
using Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods.Statistics;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public class Echo : BaseContinuousSendMethod, ISlaveMethod
    {
        public async Task<IDictionary<string, object>> Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Echo...");

                // Get parameters
                stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
                stepParameters.TryGetTypedValue(SignalRConstants.RemainderBegin, out int remainderBegin, Convert.ToInt32);
                stepParameters.TryGetTypedValue(SignalRConstants.RemainderEnd, out int remainderEnd, Convert.ToInt32);
                stepParameters.TryGetTypedValue(SignalRConstants.Modulo, out int modulo, Convert.ToInt32);
                stepParameters.TryGetTypedValue(SignalRConstants.Duration, out long duration, Convert.ToInt64);
                stepParameters.TryGetTypedValue(SignalRConstants.Interval, out long interval, Convert.ToInt64);
                stepParameters.TryGetTypedValue(SignalRConstants.MessageSize, out int messageSize, Convert.ToInt32);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionStore}.{type}",
                    out IList<IHubConnectionAdapter> connections, (obj) => (IList<IHubConnectionAdapter>)obj);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.StatisticsStore}.{type}",
                    out StatisticsCollector statisticsCollector, obj => (StatisticsCollector) obj);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionIndex}.{type}",
                    out List<int> connectionIndex, (obj) => (List<int>)obj);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionSuccessFlag}.{type}",
                    out List<SignalREnums.ConnectionState> connectionsSuccessFlag,
                    (obj) => (List<SignalREnums.ConnectionState>)obj);

                // Generate necessary data
                var data = new Dictionary<string, object>
                {
                    { SignalRConstants.MessageBlob, SignalRUtils.GenerateRandomData(messageSize) } // message payload
                };

                // Reset counters
                UpdateStatistics(statisticsCollector, remainderEnd);

                // Send messages
                await Task.WhenAll(from i in Enumerable.Range(0, connections.Count)
                                   where connectionIndex[i] % modulo >= remainderBegin && connectionIndex[i] % modulo < remainderEnd
                                   select ContinuousSend((Connection: connections[i], LocalIndex: i,
                                                          ConnectionsSuccessFlag: connectionsSuccessFlag,
                                                          StatisticsCollector: statisticsCollector), data, SendEcho,
                                                          TimeSpan.FromMilliseconds(duration),
                                                          TimeSpan.FromMilliseconds(interval),
                                                          TimeSpan.FromMilliseconds(1),
                                                          TimeSpan.FromMilliseconds(interval)));

                return null;
            }
            catch (Exception ex)
            {
                var message = $"Fail to echo: {ex}";
                Log.Error(message);
                throw;
            }
        }

        private async Task SendEcho((IHubConnectionAdapter Connection, int LocalIndex,
            List<SignalREnums.ConnectionState> ConnectionsSuccessFlag,
            StatisticsCollector StatisticsCollector) package, IDictionary<string, object> data)
        {
            try
            {
                // Is the connection is not active, then stop sending message
                if (package.ConnectionsSuccessFlag[package.LocalIndex] != SignalREnums.ConnectionState.Success) return;

                // Extract data
                data.TryGetValue(SignalRConstants.MessageBlob, out var messageBlob);

                // Prepare payload
                var payload = new Dictionary<string, object>
                {
                    { SignalRConstants.MessageBlob, messageBlob },
                    { SignalRConstants.Timestamp, Util.Timestamp() }
                };

                // Send message
                await package.Connection.SendAsync(SignalRConstants.EchoCallbackName, payload);

                // Update statistics
                SignalRUtils.RecordSend(payload, package.StatisticsCollector);
            }
            catch (Exception ex)
            {
                package.ConnectionsSuccessFlag[package.LocalIndex] = SignalREnums.ConnectionState.Fail;
                var message = $"Error in Echo: {ex}";
                Log.Error(message);
            }
        }
    }
}
