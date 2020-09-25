// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Management.ContainerService.Fluent;
using Microsoft.Azure.Management.ContainerService.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace Azure.SignalRBench.Coordinator
{
    public class AksProvider
    {
        private IAgentPoolsOperations? _agentPoolsOperations;
        private string? _resourceGroup;
        private string? _aksName;

        public void Initialize(AzureCredentials credentials, string subscription, string resourceGroup, string name)
        {
            _resourceGroup = resourceGroup;
            _aksName = name;
            var restClient = ContainerServiceManager.Authenticate(credentials, subscription).RestClient;
            var managementClient = new ContainerServiceManagementClient(restClient)
            {
                SubscriptionId = subscription
            };
            _agentPoolsOperations = managementClient.AgentPools;
        }

        public async Task CreateOrUpdateAgentPool(string agentPoolName, AgentPoolInner agentPoolInner)
        {
            if (_agentPoolsOperations == null)
            {
                throw new InvalidOperationException();
            }
            await _agentPoolsOperations.CreateOrUpdateAsync(_resourceGroup, _aksName, agentPoolName, agentPoolInner);
        }

        public Task EnsureNodeCountAsync(int poolId, int count, CancellationToken cancellationToken)
        {
            // todo
            return Task.CompletedTask;
        }

        public Task<int> GetNodePoolCountAsync()
        {
            // todo
            return Task.FromResult(1);
        }
    }
}
