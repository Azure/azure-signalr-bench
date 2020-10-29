// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Azure.SignalRBench.Common;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.Azure.Management.SignalR;
using Microsoft.Azure.Management.SignalR.Models;

namespace Azure.SignalRBench.Coordinator
{
    public class SignalRProvider : ISignalRProvider
    {
        private ISignalROperations? _signalROperations;
        private IResourceManager? _managementClient;

        public void Initialize(AzureCredentials credentials, string subscription)
        {
            var signalrManagementClient = new SignalRManagementClient(credentials);
            signalrManagementClient.SubscriptionId = subscription;
            _signalROperations = signalrManagementClient.SignalR;
            _managementClient = ResourceManager.Authenticate(credentials).WithSubscription(subscription).ResourceManager;
        }

        private ISignalROperations SignalROperations => _signalROperations ?? throw new InvalidOperationException();
        private IResourceManager ResourceManagementClient => _managementClient ?? throw new InvalidOperationException();

        public async Task CreateResourceGroupAsync(string resourceGroup, string location)
        {
            await ResourceManagementClient.ResourceGroups.Define(resourceGroup).WithRegion(location).CreateAsync();
        }

        public async Task CreateInstanceAsync(string resourceGroup, string name, string location, string tier, int size, SignalRServiceMode mode, CancellationToken cancellationToken)
        {
            var serviceMode = new SignalRFeature("ServiceMode", mode.ToString());
            var features = new List<SignalRFeature>();
            features.Add(serviceMode);
            var sku = tier.ToLower().Contains("free") ? new ResourceSku("Free_F1", "Free", "F1", capacity: 1) : new ResourceSku("Standard_S1", "Standard", "S1", capacity: size);
            var param = new SignalRResource(name: name, location: location, kind: "SignalR", sku: sku, features: features);
            await SignalROperations.BeginCreateOrUpdateAsync(resourceGroup, name, param, cancellationToken);
        }

        public async Task<string> GetKeyAsync(string resourceGroup, string name, string location, CancellationToken cancellationToken)
        {
            var signalRKeys = await SignalROperations.ListKeysAsync(resourceGroup, name, cancellationToken);
            return signalRKeys.PrimaryConnectionString;
        }

        public async Task DeleteResourceGroupAsync(string resourceGroup, string location)
        {
            await ResourceManagementClient.ResourceGroups.BeginDeleteByNameAsync(resourceGroup);
        }
    }
}
