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
    class Echo : BaseContinuousSendMethod, ISlaveMethod
    {
        public Task Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Echo...");

                // Get parameters
                PluginUtils.TryGetTypedValue(stepParameters, SignalRConstants.Type, out string type, Convert.ToString);
                PluginUtils.TryGetTypedValue(stepParameters, SignalRConstants.RemainderBegin, out int remainderBegin, Convert.ToInt32);
                PluginUtils.TryGetTypedValue(stepParameters, SignalRConstants.RemainderEnd, out int remainderEnd, Convert.ToInt32);
                PluginUtils.TryGetTypedValue(stepParameters, SignalRConstants.Modulo, out int modulo, Convert.ToInt32);
                PluginUtils.TryGetTypedValue(stepParameters, SignalRConstants.Duration, out double duration, Convert.ToDouble);
                PluginUtils.TryGetTypedValue(stepParameters, SignalRConstants.Interval, out double interval, Convert.ToDouble);
                PluginUtils.TryGetTypedValue(stepParameters, SignalRConstants.MessageSize, out int messageSize, Convert.ToInt32);
                PluginUtils.TryGetTypedValue(pluginParameters, $"{SignalRConstants.ConnectionStore}.{type}", out IList<HubConnection> connections, (obj) => (IList<HubConnection>)obj);
                PluginUtils.TryGetTypedValue(pluginParameters, $"{SignalRConstants.ConnectionOffset}.{type}", out int offset, Convert.ToInt32);

                // Set callback
                SetCallback(connections);
                
                // Generate message payload
                var data = new Dictionary<string, object>
                {
                    { SignalRConstants.MessageBlob, new byte[messageSize] }
                };

                // Send messages
                return Task.WhenAll(from i in Enumerable.Range(0, connections.Count)
                                    where i % modulo >= remainderBegin && i % modulo < remainderEnd
                                    select ContinuousSend(connections[i], data, SendEcho,
                                            TimeSpan.FromMilliseconds(duration), TimeSpan.FromMilliseconds(interval)));
            }
            catch (Exception ex)
            {
                var message = $"Fail to echo: {ex}";
                Log.Error(message);
                throw new Exception(message);
            }
        }

        public void SetCallback(IList<HubConnection> connections)
        {
            foreach (var connection in connections)
            {
                connection.On(SignalRConstants.EchoCallbackName, (IDictionary<string, object> data) =>
                {
                    Log.Information($"data: \n{data.GetContents()}");
                    var receiveTimestamp = Util.Timestamp();
                    PluginUtils.TryGetTypedValue(data, SignalRConstants.Timestamp, out long sendTimestamp, Convert.ToInt64);
                    Log.Information($"Latency: {receiveTimestamp - sendTimestamp}");
                });
            }
        }

        public async Task SendEcho(HubConnection connection, IDictionary<string, object> data)
        {
            try
            {
                var timestamp = Util.Timestamp();
                var payload = new Dictionary<string, object>();
                data.TryGetValue(SignalRConstants.MessageBlob, out var tmp);
                payload.Add(SignalRConstants.MessageBlob, tmp);
                payload.Add(SignalRConstants.Timestamp, Util.Timestamp());
                await connection.SendAsync(SignalRConstants.EchoCallbackName, payload);
            }
            catch (Exception ex)
            {
                var message = $"Error in Echo: {ex}";
                Log.Error(message);
                throw new Exception(message);
            }
        }
    }
}
