using Common;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR.Management;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public abstract class RestBase : BaseContinuousSendMethod
    {
        public async Task<IDictionary<string, object>> Do(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters)
        {
            try
            {
                ServicePointManager.DefaultConnectionLimit = SignalRConstants.DefaultConnectionLimit;
                Log.Information($"{GetType().Name} 's DefaultConnectionLimit: {ServicePointManager.DefaultConnectionLimit}");
                // Here allow manually evaluate the "send" latency if "RecordLatency" callback is not registered
                HideRecordLatency = SignalRUtils.HideMessageRoundTripLatency(stepParameters, pluginParameters);
                await RunRest(stepParameters, pluginParameters);
                return null;
            }
            catch (Exception ex)
            {
                var message = $"Fail to {GetType().Name}: {ex}";
                Log.Error(message);
                throw;
            }
        }

        public async Task<IDictionary<string, object>> RunRest(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters)
        {
            LoadParametersAndContext(stepParameters, pluginParameters);
            // Reset counters
            UpdateStatistics(StatisticsCollector, RemainderEnd);

            var hubContext = await CreateHubContextAsync();
            await RestSendMessage(hubContext);
            await hubContext.DisposeAsync();
            return null;
        }

        protected async Task SendMsgToUser(
            (string UserId,
             IServiceHubContext RestApiProvider) package,
             IDictionary<string, object> data)
        {
            try
            {
                var beforeSend = Util.Timestamp();
                var payload = GenPayload(data);
                await package.RestApiProvider
                             .Clients
                             .User(package.UserId)
                             .SendAsync(SignalRConstants.RecordLatencyCallbackName, payload);
                if (HideRecordLatency)
                {
                    var afterSend = Util.Timestamp();
                    StatisticsCollector.RecordLatency(afterSend - beforeSend);
                }
                SignalRUtils.RecordSend(payload, StatisticsCollector);
            }
            catch (Exception e)
            {
                Log.Error($"Fail to send message to user for {e.Message}");
            }
        }

        protected async Task RestSendUserMessage(IServiceHubContext hubContext)
        {
            // Generate necessary data
            var data = new Dictionary<string, object>
            {
                { SignalRConstants.MessageBlob, SignalRUtils.GenerateRandomData(MessageSize) } // message payload
            };
            await Task.WhenAll(from i in Enumerable.Range(0, ConnectionIndex.Count)
                               where ConnectionIndex[i] % Modulo >= RemainderBegin && ConnectionIndex[i] % Modulo < RemainderEnd
                               let restApiClient = hubContext
                               let userId = SignalRUtils.GenClientUserIdFromConnectionIndex(ConnectionIndex[i])
                               select ContinuousSend((UserId: userId,
                                                      RestApiClient: restApiClient),
                                                      data,
                                                      SendMsgToUser,
                                                      TimeSpan.FromMilliseconds(Duration),
                                                      TimeSpan.FromMilliseconds(Interval),
                                                      TimeSpan.FromMilliseconds(1),
                                                      TimeSpan.FromMilliseconds(Interval)));
        }

        protected async Task RestBroadcastMessage(IServiceHubContext hubContext)
        {
            // Generate necessary data
            var data = new Dictionary<string, object>
            {
                { SignalRConstants.MessageBlob, SignalRUtils.GenerateRandomData(MessageSize) } // message payload
            };
            // Send messages
            await Task.WhenAll(from i in Enumerable.Range(0, ConnectionIndex.Count)
                               where ConnectionIndex[i] % Modulo >= RemainderBegin && ConnectionIndex[i] % Modulo < RemainderEnd
                               let restApiClient = hubContext
                               select ContinuousSend(restApiClient,
                                                     data,
                                                     BroadcastMsg,
                                                     TimeSpan.FromMilliseconds(Duration),
                                                     TimeSpan.FromMilliseconds(Interval),
                                                     TimeSpan.FromMilliseconds(1),
                                                     TimeSpan.FromMilliseconds(Interval)));
        }

        protected async Task BroadcastMsg(
             IServiceHubContext restApiProvider,
             IDictionary<string, object> data)
        {
            try
            {
                var payload = GenPayload(data);
                await restApiProvider
                             .Clients
                             .All.SendAsync(SignalRConstants.RecordLatencyCallbackName, payload);
                SignalRUtils.RecordSend(payload, StatisticsCollector);
            }
            catch (Exception e)
            {
                Log.Error($"Fail to broadcast message for {e.Message}");
            }
        }

        protected Task<IServiceHubContext> CreateHubContextHelperAsync(ServiceTransportType serviceTransportType)
        {
            return SignalRUtils.CreateHubContextHelperAsync(serviceTransportType, PluginParameters, Type);
        }

        protected abstract Task<IServiceHubContext> CreateHubContextAsync();

        protected abstract Task RestSendMessage(IServiceHubContext hubContext);
    }
}
