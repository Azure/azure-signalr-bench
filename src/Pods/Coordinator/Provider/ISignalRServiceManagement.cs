// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace Azure.SignalRBench.Coordinator
{
    public interface ISignalRServiceManagement
    {
        Task CreateInstanceAsync(string resourceGroup, string name, string location, string tier, int size, string tags,
            SignalRServiceMode mode, CancellationToken cancellationToken);

        Task CreateResourceGroupAsync(string resourceGroup);
        Task DeleteResourceGroupAsync(string resourceGroup);

        Task<string> GetKeyAsync(string resourceGroup, string name, 
            CancellationToken cancellationToken);

        void Initialize(AzureCredentials credentials, string subscription,string location,string prefix);
    }
}