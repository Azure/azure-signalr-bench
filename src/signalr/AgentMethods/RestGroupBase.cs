using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR.Management;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.AgentMethods
{
    public abstract class RestGroupBase : SendToGroupBase
    {
        protected override async Task SendMessages(IEnumerable<Package> packages)
        {
            var hubContext = await CreateHubContextAsync();
            await Task.WhenAll(from package in packages
                               let index = ConnectionIndex[package.LocalIndex]
                               let groupIndex = index % GroupCount
                               let indexInGroup = index / GroupCount
                               let connection = package.Connection
                               let data = package.Data
                               let restApiClient = hubContext
                               where Mode == SignalREnums.GroupConfigMode.Group
                                     && IsSending(indexInGroup, GroupInternalModulo, GroupInternalRemainderBegin, GroupInternalRemainderEnd)
                                     && IsSending(groupIndex, GroupCount, GroupLevelRemainderBegin, GroupLevelRemainderEnd)
                                     || Mode == SignalREnums.GroupConfigMode.Connection
                                     && IsSending(index, Modulo, RemainderBegin, RemainderEnd)
                               select ContinuousSend((GroupName: SignalRUtils.GroupName(Type, index % GroupCount),
                                                      RestApiProvider: restApiClient),
                                                      data,
                                                      SendToGroup,
                                                      TimeSpan.FromMilliseconds(Duration),
                                                      TimeSpan.FromMilliseconds(Interval),
                                                      TimeSpan.FromMilliseconds(1),
                                                      TimeSpan.FromMilliseconds(Interval)));
        }

        private async Task SendToGroup(
            (string GroupName,
             IServiceHubContext RestApiProvider) package,
             IDictionary<string, object> data)
        {
            try
            {
                var payload = GenPayload(data);
                await package.RestApiProvider
                     .Clients
                     .Group(package.GroupName)
                     .SendAsync(SignalRConstants.RecordLatencyCallbackName, payload);
                SignalRUtils.RecordSend(payload, StatisticsCollector);
            }
            catch (Exception e)
            {
                Log.Error($"Fail to send message to group for {e.Message}");
            }
        }

        protected Task<IServiceHubContext> CreateHubContextHelperAsync(ServiceTransportType serviceTransportType)
        {
            return SignalRUtils.CreateHubContextHelperAsync(serviceTransportType, PluginParameters, Type);
        }

        protected abstract Task<IServiceHubContext> CreateHubContextAsync();
    }
}
