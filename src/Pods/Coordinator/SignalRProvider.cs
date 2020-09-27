// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

using Azure.SignalRBench.Common;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.SignalR;

namespace Azure.SignalRBench.Coordinator
{
    public class SignalRProvider
    {
        private ISignalROperations? _signalROperations;

        public void Initialize(AzureCredentials credentials, string subscription)
        {
            var signalrManagementClient = new SignalRManagementClient(credentials);
            signalrManagementClient.SubscriptionId = subscription;
            _signalROperations = signalrManagementClient.SignalR;
        }

        private ISignalROperations SignalROperations => _signalROperations ?? throw new InvalidOperationException();

        public Task CreateInstanceAsync(string resourceGroup, string name, string location, string tier, int size, SignalRServiceMode mode, CancellationToken cancellationToken)
        {
            // todo
            return Task.CompletedTask;
        }

        public Task<string> CreateKeyAsync(string resourceGroup, string name, string location, CancellationToken cancellationToken)
        {
            // todo
            return Task.FromResult(string.Empty);
        }

        public Task DeleteResourceGroupAsync(string resourceGroup, string location)
        {
            // todo
            return Task.CompletedTask;
        }
    }
}
