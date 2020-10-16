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
    public class AksProvider : IAksProvider
    {
        private IAgentPoolsOperations? _client;
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
            _client = managementClient.AgentPools;
        }

        public async Task CreateOrUpdateAgentPool(int nodePoolIndex, string vmSize, int nodeCount, string osType, CancellationToken cancellationToken)
        {
            if (_client == null)
            {
                throw new InvalidOperationException();
            }
            await _client.CreateOrUpdateAsync(
                _resourceGroup,
                _aksName,
                ToPoolName(nodePoolIndex),
                new AgentPoolInner
                {
                    AgentPoolType = AgentPoolType.VirtualMachineScaleSets,
                    Count = nodeCount,
                    EnableAutoScaling = false,
                    EnableNodePublicIP = true,
                    OsType = OSType.Parse(osType),
                    VmSize = ContainerServiceVMSizeTypes.Parse(vmSize),
                    NodeTaints = new[] { $"pool:{nodePoolIndex}" },
                },
                cancellationToken);
        }

        public async Task EnsureNodeCountAsync(int nodePoolIndex, int nodeCount, CancellationToken cancellationToken)
        {
            if (_client == null)
            {
                throw new InvalidOperationException();
            }
            var pool = await _client.GetAsync(_resourceGroup, _aksName, ToPoolName(nodePoolIndex), cancellationToken);
            if (pool.Count == nodeCount)
            {
                return;
            }
            await CreateOrUpdateAgentPool(nodePoolIndex, pool.VmSize.Value, nodeCount, pool.OsType.Value, cancellationToken);
        }

        public Task<int> GetNodePoolCountAsync(CancellationToken cancellationToken)
        {
            if (_client == null)
            {
                throw new InvalidOperationException();
            }
            // todo
            return Task.FromResult(1);
        }

        private string ToPoolName(int nodePoolIndex) => $"Pool{nodePoolIndex}";
    }
}
