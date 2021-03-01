// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.SignalR;
using Microsoft.Azure.Management.SignalR.Models;

namespace Azure.SignalRBench.Coordinator
{
    public class SignalRServiceManagement : ISignalRServiceManagement
    {
        private IResourceManager? _managementClient;
        private ISignalROperations? _signalROperations;
        private string _location;
        private string _prefix;

        public ISignalROperations SignalROperations => _signalROperations ?? throw new InvalidOperationException();

        public IResourceManager ResourceManagementClient => _managementClient ?? throw new InvalidOperationException();

        public void Initialize(AzureCredentials credentials, string subscription, string location, string prefix)
        {
            var signalrManagementClient = new SignalRManagementClient(credentials)
            {
                SubscriptionId = subscription
            };
            signalrManagementClient.BaseUri = new Uri(credentials.Environment.ResourceManagerEndpoint);
            _signalROperations = signalrManagementClient.SignalR;
            _managementClient = ResourceManager.Authenticate(credentials).WithSubscription(subscription)
                .ResourceManager;
            _location = location;
            _prefix = prefix;
        }

        public Task CreateResourceGroupAsync(string resourceGroup)
        {
            return ResourceManagementClient.ResourceGroups.Define(resourceGroup).WithRegion(_location).CreateAsync();
        }

        public async Task CreateInstanceAsync(string resourceGroup, string name, string location, string tier, int size,
            string tags,
            SignalRServiceMode mode, CancellationToken cancellationToken)
        {
            var serviceMode = new SignalRFeature("ServiceMode", mode.ToString());
            var features = new List<SignalRFeature> {serviceMode};
            var sku = GetSku(tier, size);
            var tagsParam = new Dictionary<string, string>();
            foreach (var str in tags.Split(";"))
            {
                var kv = str.Split("=");
                if (kv.Length == 2) tagsParam[kv[0]] = kv[1];
            }

            ServerlessUpstreamSettings upstreamSettings = null;
            if (mode == SignalRServiceMode.Serverless)
            {
                upstreamSettings = new ServerlessUpstreamSettings();
                var list = new List<UpstreamTemplate>()
                {
                    new UpstreamTemplate(
                        GetUpstream())
                };
                upstreamSettings.Templates = list;
            }

            var param = new SignalRResource(name: name, location: location, kind: "SignalR", sku: sku, tags: tagsParam,
                features: features, upstream: upstreamSettings);
            await SignalROperations.CreateOrUpdateAsync(resourceGroup, name, param, cancellationToken);
        }

        public async Task<string> GetKeyAsync(string resourceGroup, string name,
            CancellationToken cancellationToken)
        {
            var signalRKeys = await SignalROperations.ListKeysAsync(resourceGroup, name, cancellationToken);
            return signalRKeys.PrimaryConnectionString;
        }

        public async Task DeleteResourceGroupAsync(string resourceGroup)
        {
            await ResourceManagementClient.ResourceGroups.BeginDeleteByNameAsync(resourceGroup);
        }

        private static ResourceSku GetSku(string tier, int size)
        {
            return tier.ToLower().Contains("free")
                ? new ResourceSku("Free_F1", "Free", "F1", capacity: 1)
                : new ResourceSku("Standard_S1", "Standard", "S1", capacity: size);
        }

        private string GetUpstream()
        {
            return "https://" + _prefix + "perfv2." + _location +
                   ".cloudapp.azure.com/upstream/{hub}/api/{category}/{event}";
        }
    }
}