using Microsoft.Azure.SignalR.Management;
using Plugin.Base;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public class PersistBroadcast : RestBase, ISlaveMethod
    {
        protected override Task<IServiceHubContext> CreateHubContextAsync()
        {
            return CreateHubContextHelperAsync(ServiceTransportType.Persistent);
        }

        protected override async Task RestSendMessage(IServiceHubContext hubContext)
        {
            await RestBroadcastMessage(hubContext);
        }
    }
}
