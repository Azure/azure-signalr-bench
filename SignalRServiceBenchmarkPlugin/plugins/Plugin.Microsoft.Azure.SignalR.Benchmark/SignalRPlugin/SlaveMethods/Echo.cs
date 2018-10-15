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

                // Set callback
                SetCallback(connections);
                
                // Generate message payload
                var data = new Dictionary<string, object>
                {
                    { SignalRConstants.MessageBlob, new byte[messageSize] }
                };

                // Send messages
                return Task.WhenAll(from connection in connections
                                    select ContinuousSend(connection, data, SendEcho, 
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
            _ = from connection in connections
                select connection.On(SignalRConstants.EchoCallbackName, (IDictionary<string, object> data) => 
                {
                    var receiveTimestamp = Util.Timestamp();
                    PluginUtils.TryGetTypedValue(data, SignalRConstants.Timestamp, out long sendTimestamp, Convert.ToInt64);
                    Log.Information($"{receiveTimestamp - sendTimestamp}");
                });
        }

        public Task SendEcho(HubConnection connection, IDictionary<string, object> data)
        {
            var timestamp = Util.Timestamp();
            data[SignalRConstants.Timestamp] = timestamp;
            return connection.SendAsync(SignalRConstants.EchoCallbackName, data);
        }
    }
}
