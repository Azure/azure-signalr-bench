// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace Azure.SignalRBench.Coordinator
{
    public interface IAksProvider
    {
        Task CreateOrUpdateAgentPool(int nodePoolIndex, string vmSize, int nodeCount, string osType,
            CancellationToken cancellationToken);

        Task EnsureNodeCountAsync(int nodePoolIndex, int nodeCount, CancellationToken cancellationToken);
        Task<int> GetNodePoolCountAsync(CancellationToken cancellationToken);
        void Initialize(AzureCredentials credentials, string subscription, string resourceGroup, string name);
    }
}