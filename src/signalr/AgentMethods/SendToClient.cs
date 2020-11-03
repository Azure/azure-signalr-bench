using Common;
using Plugin.Base;
using Plugin.Microsoft.Azure.SignalR.Benchmark.AgentMethods.Statistics;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.AgentMethods
{
    public class SendToClient : BaseContinuousSendMethod, IAgentMethod
    {
        public async Task<IDictionary<string, object>> Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Send {GetType().Name}...");

                LoadParametersAndContext(stepParameters, pluginParameters);
                // Get parameters
                stepParameters.TryGetTypedValue(SignalRConstants.ConnectionIdStore, out string[] connectionIds,
                    obj => Convert.ToString(obj).Split(' '));

                // Generate necessary data
                var messageBlob = SignalRUtils.GenerateRandomData(MessageSize);

                // Reset counters
                UpdateStatistics(StatisticsCollector, RemainderEnd);

                // Send messages
                await Task.WhenAll(
                    from i in Enumerable.Range(0, Connections.Count)
                    let data = new BenchMessage { MessageBlob = messageBlob, Target = connectionIds[i] }
                    where ConnectionIndex[i] % Modulo >= RemainderBegin && ConnectionIndex[i] % Modulo < RemainderEnd
                    select ContinuousSend(
                        (Connection: Connections[i], LocalIndex: i, CallbackMethod: SignalRConstants.SendToClientCallbackName),
                        data,
                        BaseSendAsync,
                        TimeSpan.FromMilliseconds(Duration),
                        TimeSpan.FromMilliseconds(Interval),
                        TimeSpan.FromMilliseconds(1),
                        TimeSpan.FromMilliseconds(Interval)));
                Log.Information($"Finish {GetType().Name} {RemainderEnd}");
                return null;
            }
            catch (Exception ex)
            {
                var message = $"Fail to send to client: {ex}";
                Log.Error(message);
                throw;
            }
        }
    }
}
