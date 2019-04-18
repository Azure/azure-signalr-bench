using Microsoft.Azure.SignalR.Management;
using Plugin.Base;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public class PersistSendToUser : RestBase, ISlaveMethod
    {
        protected override Task<IServiceHubContext> CreateHubContextAsync()
        {
            return CreateHubContextHelperAsync(ServiceTransportType.Persistent);
        }

        protected override async Task RestSendMessage(IServiceHubContext hubContext)
        {
            await RestSendUserMessage(hubContext);
        }
    }
}
