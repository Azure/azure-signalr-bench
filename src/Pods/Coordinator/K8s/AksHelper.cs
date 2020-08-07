using Microsoft.Azure.Management.ContainerService.Fluent;
using Microsoft.Azure.Management.ContainerService.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using System.Threading.Tasks;

namespace Coordinator
{
    class AksHelper
    {
        private IAgentPoolsOperations agentPoolsOperations;

        public AksHelper()
        {
            agentPoolsOperations = getAgentPool();
        }

        private IAgentPoolsOperations getAgentPool()
        {
            var restClient = ContainerServiceManager.Authenticate(PerfConfig.SERVICE_PRINCIPAL, PerfConfig.SUBSCRIPTION).RestClient;
            var managementClient = new ContainerServiceManagementClient(restClient)
            {
                SubscriptionId = PerfConfig.SUBSCRIPTION
            };
            return managementClient.AgentPools;
        }

        public async Task CreateOrUpdateAgentPool(string agentPoolName, AgentPoolInner agentPoolInner)
        {
            await agentPoolsOperations.CreateOrUpdateAsync(PerfConfig.RESOUCE_GROUP, PerfConfig.AKS, agentPoolName,
              agentPoolInner);
        }
    }
}
