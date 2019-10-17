using Common;
using Plugin.Base;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.AgentMethods
{
    public class StreamingEcho : BaseStreamingSendMethod, IAgentMethod
    {
        public Task<IDictionary<string, object>> Do(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters)
        {
            return SimpleScenarioSend(stepParameters, pluginParameters, SignalRConstants.StreamingEchoCallbackName);
        }

        protected override async Task<IDictionary<string, object>> SimpleScenarioSend(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters,
            string callbackMethod)
        {
            try
            {
                Log.Information($"Start {GetType().Name}...");

                LoadParametersAndContext(stepParameters, pluginParameters);

                // Generate necessary data
                var data = new Dictionary<string, object>
                {
                    { SignalRConstants.MessageBlob, SignalRUtils.GenerateRandomData(MessageSize) } // message payload
                };

                // Reset counters
                UpdateStatistics(StatisticsCollector, RemainderEnd);

                // Send messages
                await Task.WhenAll(from i in Enumerable.Range(0, Connections.Count)
                                   where ConnectionIndex[i] % Modulo >= RemainderBegin && ConnectionIndex[i] % Modulo < RemainderEnd
                                   select ContinuousSend((Connection: Connections[i],
                                                          LocalIndex: i,
                                                          CallbackMethod: callbackMethod,
                                                          StreamItemsCount,
                                                          StreamItemInterval),
                                                          data,
                                                          StreamingBaseSendAsync,
                                                          TimeSpan.FromMilliseconds(Duration),
                                                          TimeSpan.FromMilliseconds(Interval),
                                                          TimeSpan.FromMilliseconds(1),
                                                          TimeSpan.FromMilliseconds(Interval)));
                Log.Information($"Finish {GetType().Name} {RemainderEnd}");
                return null;
            }
            catch (Exception ex)
            {
                var message = $"Fail to {GetType().Name}: {ex}";
                Log.Error(message);
                throw;
            }
        }

        protected async Task StreamingBaseSendAsync(
            (IHubConnectionAdapter Connection,
            int LocalIndex,
            string CallbackMethod,
            int streamCount,
            int streamItemInterval) package,
            IDictionary<string, object> data)
        {
            async Task StreamingWriter(ChannelWriter<IDictionary<string, object>> writer, IDictionary<string, object> sentData, int count, int streamItemWaiting)
            {
                Exception localException = null;
                try
                {
                    for (var i = 0; i < count; i++)
                    {
                        sentData[SignalRConstants.Timestamp] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                        await writer.WriteAsync(sentData);
                        if (streamItemWaiting > 0)
                        {
                            await Task.Delay(streamItemWaiting);
                        }
                        // Update statistics
                        SignalRUtils.RecordSend(sentData, StatisticsCollector);
                    }
                }
                catch (Exception ex)
                {
                    localException = ex;
                }
                writer.Complete(localException);
            }

            try
            {
                // Is the connection is not active, then stop sending message
                if (package.Connection.GetStat() != SignalREnums.ConnectionInternalStat.Active)
                    return;
                var payload = GenPayload(data);
                var channel = Channel.CreateUnbounded<IDictionary<string, object>>();
                _ = StreamingWriter(channel.Writer, payload, package.streamCount, package.streamItemInterval);
                using (var c = new CancellationTokenSource(TimeSpan.FromSeconds(5 * package.streamCount)))
                {
                    int recvCount = 0;
                    var stream = await package.Connection.StreamAsChannelAsync<IDictionary<string, object>>(package.CallbackMethod, channel.Reader, package.streamItemInterval, c.Token);
                    while (await stream.WaitToReadAsync(c.Token))
                    {
                        while (stream.TryRead(out var item))
                        {
                            var receiveTimestamp = Util.Timestamp();
                            if (item.TryGetValue(SignalRConstants.Timestamp, out object v))
                            {
                                var value = v.ToString();
                                var sendTimestamp = Convert.ToInt64(value);
                                var latency = receiveTimestamp - sendTimestamp;
                                StatisticsCollector.RecordLatency(latency);
                                SignalRUtils.RecordRecvSize(item, StatisticsCollector);
                                recvCount++;
                            }
                        }
                    }
                    if (recvCount < package.streamCount)
                    {
                        Log.Error($"The received streaming items {recvCount} is not equal to sending items {package.streamCount}");
                        StatisticsCollector.IncreaseStreamItemMissing(1);
                    }
                }
                
            }
            catch (Exception ex)
            {
                var message = $"Error in {GetType().Name}: {ex}";
                Log.Error(message);
            }
        }
    }
}
