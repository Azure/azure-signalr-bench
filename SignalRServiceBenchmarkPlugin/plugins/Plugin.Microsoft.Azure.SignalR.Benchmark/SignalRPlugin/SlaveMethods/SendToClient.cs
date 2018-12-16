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
    public class SendToClient : BaseContinuousSendMethod, ISlaveMethod
    {
        public async Task<IDictionary<string, object>> Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Send to client...");

                // Get parameters
                stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
                stepParameters.TryGetTypedValue(SignalRConstants.RemainderBegin, out int remainderBegin, Convert.ToInt32);
                stepParameters.TryGetTypedValue(SignalRConstants.RemainderEnd, out int remainderEnd, Convert.ToInt32);
                stepParameters.TryGetTypedValue(SignalRConstants.Modulo, out int modulo, Convert.ToInt32);
                stepParameters.TryGetTypedValue(SignalRConstants.Duration, out long duration, Convert.ToInt64);
                stepParameters.TryGetTypedValue(SignalRConstants.Interval, out long interval, Convert.ToInt64);
                stepParameters.TryGetTypedValue(SignalRConstants.MessageSize, out int messageSize, Convert.ToInt32);
                stepParameters.TryGetTypedValue(SignalRConstants.ConnectionIdStore, out string[] connectionIds, obj => Convert.ToString(obj).Split(' '));

                // Get context
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionStore}.{type}",
                    out IList<IHubConnectionAdapter> connections, (obj) => (IList<IHubConnectionAdapter>)obj);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.StatisticsStore}.{type}",
                    out StatisticsCollector statisticsCollector, obj => (StatisticsCollector) obj);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionIndex}.{type}",
                    out List<int> connectionIndex, (obj) => (List<int>)obj);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionSuccessFlag}.{type}",
                    out List<SignalREnums.ConnectionState> connectionsSuccessFlag, (obj) => (List<SignalREnums.ConnectionState>)obj);

                // Generate necessary data
                var messageBlob = new byte[messageSize];

                var packages = from i in Enumerable.Range(0, connections.Count)
                               select new
                               {
                                   Index = i,
                                   Connection = connections[i],
                                   Data = new Dictionary<string, object>
                                   {
                                       { SignalRConstants.MessageBlob, messageBlob }, // message payload
                                       { SignalRConstants.ConnectionId, connectionIds[i]}
                                   }
                               };

                // Reset counters
                SignalRUtils.ResetCounters(statisticsCollector);
                statisticsCollector.SetSendingStep(remainderEnd);
                // Send messages
                await Task.WhenAll(from package in packages
                                   let i = package.Index
                                   let connection = package.Connection
                                   let data = package.Data
                                   where connectionIndex[i] % modulo >= remainderBegin && connectionIndex[i] % modulo < remainderEnd
                                   select ContinuousSend((Connection: connections[i], LocalIndex: i, ConnectionsSuccessFlag: connectionsSuccessFlag, StatisticsCollector: statisticsCollector), data, SendClient,
                                        TimeSpan.FromMilliseconds(duration), TimeSpan.FromMilliseconds(interval),
                                        TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(interval)));

                return null;
            }
            catch (Exception ex)
            {
                var message = $"Fail to send to client: {ex}";
                Log.Error(message);
                throw;
            }
        }

        private async Task SendClient((IHubConnectionAdapter Connection, int LocalIndex,
            List<SignalREnums.ConnectionState> ConnectionsSuccessFlag,
            StatisticsCollector StatisticsCollector) package, IDictionary<string, object> data)
        {
            try
            {
                // Extract data
                data.TryGetTypedValue(SignalRConstants.ConnectionId, out string targetId, Convert.ToString);
                data.TryGetValue(SignalRConstants.MessageBlob, out var messageBlob);

                // Generate payload
                var payload = new Dictionary<string, object>
                {
                    { SignalRConstants.MessageBlob, messageBlob },
                    { SignalRConstants.Timestamp, Util.Timestamp() },
                    { SignalRConstants.ConnectionId, targetId }
                };

                // Send message
                await package.Connection.SendAsync(SignalRConstants.SendToClientCallbackName, payload);

                // Update statistics
                package.StatisticsCollector.IncreaseSentMessage();
            }
            catch (Exception ex)
            {
                var message = $"Error in send to client: {ex}";
                Log.Error(message);
                throw;
            }
        }
    }
}
