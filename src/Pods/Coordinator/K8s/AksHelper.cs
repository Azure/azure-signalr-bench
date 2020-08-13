// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Management.ContainerService.Fluent;
using Microsoft.Azure.Management.ContainerService.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using System.Threading.Tasks;

namespace Coordinator
{
    internal class AksHelper
    {
        private IAgentPoolsOperations agentPoolsOperations;

        public AksHelper()
        {
            agentPoolsOperations = getAgentPool();
        }

        private IAgentPoolsOperations getAgentPool()
        {
            var restClient = ContainerServiceManager.Authenticate(PerfConfig.ServicePrincipal, PerfConfig.Subscription).RestClient;
            var managementClient = new ContainerServiceManagementClient(restClient)
            {
                SubscriptionId = PerfConfig.Subscription
            };
            return managementClient.AgentPools;
        }

        public async Task CreateOrUpdateAgentPool(string agentPoolName, AgentPoolInner agentPoolInner)
        {
            await agentPoolsOperations.CreateOrUpdateAsync(PerfConfig.ResourceGroup, PerfConfig.AKS, agentPoolName,
              agentPoolInner);
        }
    }
}
