using Microsoft.Azure.SignalR.Management;
using Plugin.Base;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.AgentMethods
{
    public class RestSendToGroup : RestGroupBase, IAgentMethod
    {
        protected override Task<IServiceHubContext> CreateHubContextAsync()
        {
            return CreateHubContextHelperAsync(ServiceTransportType.Transient);
        }
    }
}
