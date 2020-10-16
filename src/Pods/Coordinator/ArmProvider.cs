// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using MAzure = Microsoft.Azure.Management.Fluent.Azure;

namespace Azure.SignalRBench.Coordinator
{
    public class ArmProvider : IArmProvider
    {
        private string? _resourceGroup;
        private IAzure? _azure;

        public void Initialize(AzureCredentials credentials, string subscription, string resourceGroup)
        {
            _resourceGroup = resourceGroup;
            _azure = MAzure.Configure().Authenticate(credentials).WithSubscription(subscription);
        }

        public IAzure Azure => _azure ?? throw new InvalidOperationException();

        public async Task Deploy(string deploymentName, JObject deployTemplate, JObject deployParams)
        {
            await Azure
                .Deployments
                .Define(deploymentName)
                .WithExistingResourceGroup(_resourceGroup ?? throw new InvalidOperationException())
                .WithTemplate(JsonConvert.SerializeObject(deployTemplate))
                .WithParameters(deployParams)
                .WithMode(DeploymentMode.Incremental)
                .BeginCreateAsync();
        }
    }
}
