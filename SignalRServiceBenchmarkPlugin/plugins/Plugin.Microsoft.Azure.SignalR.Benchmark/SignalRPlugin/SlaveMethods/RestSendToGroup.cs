using Microsoft.Azure.SignalR.Management;
using Plugin.Base;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public class RestSendToGroup : RestGroupBase, ISlaveMethod
    {
        protected override Task<IServiceHubContext> CreateHubContextAsync()
        {
            return CreateHubContextHelperAsync(ServiceTransportType.Transient);
        }
    }
}
