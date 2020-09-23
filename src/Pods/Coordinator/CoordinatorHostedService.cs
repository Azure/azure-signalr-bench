// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Azure.Security.KeyVault.Secrets;
using Azure.SignalRBench.Common;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Azure.SignalRBench.Coordinator
{
    internal class CoordinatorHostedService : IHostedService
    {
        private readonly SecretClient _secretClient;
        private readonly K8sProvider _k8sProvider;
        private readonly AksProvider _aksProvider;
        private readonly ArmProvider _armProvider;
        private readonly SignalRProvider _signalRProvider;
        private readonly PerfStorageProvider _storageProvider;
        private readonly TestScheduler _scheduler;

        public CoordinatorHostedService(
            SecretClient secretClient,
            PerfStorageProvider storageProvider,
            K8sProvider k8sProvider,
            AksProvider aksProvider,
            ArmProvider armProvider,
            SignalRProvider signalRProvider,
            TestScheduler scheduler)
        {
            _secretClient = secretClient;
            _storageProvider = storageProvider;
            _k8sProvider = k8sProvider;
            _aksProvider = aksProvider;
            _armProvider = armProvider;
            _signalRProvider = signalRProvider;
            _scheduler = scheduler;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var storageTask = _secretClient.GetSecretAsync(Constants.KeyVaultKeys.StorageConnectionStringKey);

            var prefixTask = _secretClient.GetSecretAsync(Constants.KeyVaultKeys.PrefixKey);
            var subscriptionTask = _secretClient.GetSecretAsync(Constants.KeyVaultKeys.SubscriptionKey);
            //var locationTask = _secretClient.GetSecretAsync(Constants.KeyVaultKeys.LocationKey);
            var servicePrincipalTask = _secretClient.GetSecretAsync(Constants.KeyVaultKeys.ServicePrincipalKey);
            var cloudTask = _secretClient.GetSecretAsync(Constants.KeyVaultKeys.CloudKey);
            var k8sTask = _secretClient.GetSecretAsync(Constants.KeyVaultKeys.KubeConfigKey);

            _storageProvider.Initialize((await storageTask).Value.Value);
            _k8sProvider.Initialize((await k8sTask).Value.Value);
            var prefix = (await prefixTask).Value.Value;
            var subscription = (await subscriptionTask).Value.Value;
            var azureEnvironment = GetAzureEnvironment((await cloudTask).Value.Value);
            var obj = JsonConvert.DeserializeObject<JObject>((await servicePrincipalTask).Value.Value);
            if (obj == null)
            {
                throw new InvalidDataException("Unexpected null for service principal.");
            }
            var servicePrincipal = SdkContext.AzureCredentialsFactory.FromServicePrincipal(
                obj["appId"]?.Value<string>() ?? throw new InvalidDataException("Unexpected null for ServicePrincipal.AppId."),
                obj["password"]?.Value<string>() ?? throw new InvalidDataException("Unexpected null for ServicePrincipal.Password."),
                obj["tenant"]?.Value<string>() ?? throw new InvalidDataException("Unexpected null for ServicePrincipal.Tenant."),
                azureEnvironment);

            _aksProvider.Initialize(servicePrincipal, subscription, prefix + "rg", prefix + "aks");
            _armProvider.Initialize(servicePrincipal, subscription, prefix + "rg");
            _signalRProvider.Initialize(servicePrincipal, subscription);
            _scheduler.Start(await _storageProvider.Storage.GetQueueAsync<TestJob>(Constants.QueueNames.PortalJob, true));
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _scheduler.StopAsync();
        }

        private static AzureEnvironment GetAzureEnvironment(string name)
        {
            switch (name)
            {
                case "AzureCloud":
                    return AzureEnvironment.FromName("AzureGlobalCloud");
                case "PPE":
                    return new AzureEnvironment
                    {
                        GraphEndpoint = "https://graph.ppe.windows.net/",
                        AuthenticationEndpoint = "https://login.windows-ppe.net",
                        Name = "PPE",
                        ManagementEndpoint = "https://umapi-preview.core.windows-int.net/",
                        ResourceManagerEndpoint = "https://api-dogfood.resources.windows-int.net/"
                    };
                default:
                    return AzureEnvironment.FromName(name) ?? throw new InvalidDataException("Unknown azure cloud name.");
            }
        }
    }
}
