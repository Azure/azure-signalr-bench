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
    public class CoordinatorHostedService : IHostedService
    {
        private readonly SecretClient _secretClient;
        private readonly IK8sProvider _k8sProvider;
        private readonly IAksProvider _aksProvider;
        private readonly PerfStorageProvider _storageProvider;
        private readonly TestScheduler _scheduler;
        private readonly SignalRProviderHolder _signalRProviderHolder;

        public CoordinatorHostedService(
            SecretClient secretClient,
            PerfStorageProvider storageProvider,
            IK8sProvider k8sProvider,
            IAksProvider aksProvider,
            SignalRProviderHolder signalRProviderHolder,
            TestScheduler scheduler)
        {
            _secretClient = secretClient;
            _storageProvider = storageProvider;
            _k8sProvider = k8sProvider;
            _aksProvider = aksProvider;
            _signalRProviderHolder = signalRProviderHolder;
            _scheduler = scheduler;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var storageTask = _secretClient.GetSecretAsync(PerfConstants.KeyVaultKeys.StorageConnectionStringKey);

            var prefixTask = _secretClient.GetSecretAsync(PerfConstants.KeyVaultKeys.PrefixKey);
            var subscriptionTask = _secretClient.GetSecretAsync(PerfConstants.KeyVaultKeys.SubscriptionKey);
            var ppeSubscriptionTask = _secretClient.GetSecretAsync(PerfConstants.KeyVaultKeys.PPESubscriptionKey);
            var locationTask = _secretClient.GetSecretAsync(PerfConstants.KeyVaultKeys.LocationKey);
            var servicePrincipalTask = _secretClient.GetSecretAsync(PerfConstants.KeyVaultKeys.ServicePrincipalKey);
            var ppeServicePrincipalTask = _secretClient.GetSecretAsync(PerfConstants.KeyVaultKeys.PPEServicePrincipalKey);
            var cloudTask = _secretClient.GetSecretAsync(PerfConstants.KeyVaultKeys.CloudKey);
            var k8sTask = _secretClient.GetSecretAsync(PerfConstants.KeyVaultKeys.KubeConfigKey);
            _storageProvider.Initialize((await storageTask).Value.Value);
            _k8sProvider.Initialize((await k8sTask).Value.Value);
            var prefix = (await prefixTask).Value.Value;
            var subscription = (await subscriptionTask).Value.Value;
            var azureEnvironment = GetAzureEnvironment((await cloudTask).Value.Value);
            var obj = JsonConvert.DeserializeObject<JObject>((await servicePrincipalTask).Value.Value) ??
                throw new InvalidDataException("Unexpected null for service principal.");
            var servicePrincipal = SdkContext.AzureCredentialsFactory.FromServicePrincipal(
                obj["appId"]?.Value<string>() ?? throw new InvalidDataException("Unexpected null for ServicePrincipal.AppId."),
                obj["password"]?.Value<string>() ?? throw new InvalidDataException("Unexpected null for ServicePrincipal.Password."),
                obj["tenant"]?.Value<string>() ?? throw new InvalidDataException("Unexpected null for ServicePrincipal.Tenant."),
                azureEnvironment);

            _aksProvider.Initialize(servicePrincipal, subscription, prefix + PerfConstants.ConfigurationKeys.PerfV2 + "rg", prefix + PerfConstants.ConfigurationKeys.PerfV2 + "aks");
            var azureGlobalSignalrProvider=new SignalRProvider();
            azureGlobalSignalrProvider.Initialize(servicePrincipal, subscription);
            _signalRProviderHolder.AzureGlobal = azureGlobalSignalrProvider;
            //init ppe if available 
            try
            {
                var ppeSubscription = (await ppeSubscriptionTask).Value.Value;
                if (ppeSubscription != null)
                {
                    var ppeObj = JsonConvert.DeserializeObject<JObject>((await ppeServicePrincipalTask).Value.Value) ??
                                 throw new InvalidDataException("Unexpected null for service principal.");
                    var ppeServicePrincipal = SdkContext.AzureCredentialsFactory.FromServicePrincipal(
                        ppeObj["appId"]?.Value<string>() ??
                        throw new InvalidDataException("Unexpected null for ServicePrincipal.AppId."),
                        ppeObj["password"]?.Value<string>() ??
                        throw new InvalidDataException("Unexpected null for ServicePrincipal.Password."),
                        ppeObj["tenant"]?.Value<string>() ??
                        throw new InvalidDataException("Unexpected null for ServicePrincipal.Tenant.")
                       , GetAzureEnvironment("PPE"));
                    var ppeSignalrProvider = new SignalRProvider();
                    ppeSignalrProvider.Initialize(ppeServicePrincipal, ppeSubscription);
                    _signalRProviderHolder.PPE = ppeSignalrProvider;
                }
            }
            catch (Exception)
            {
                // ignored
            }

            await _scheduler.StartAsync((await locationTask).Value.Value);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _scheduler.StopAsync();
        }

        private static AzureEnvironment GetAzureEnvironment(string name) =>
            name switch
            {
                "AzureCloud" => AzureEnvironment.FromName("AzureGlobalCloud"),
                "PPE" =>  new AzureEnvironment
                {
                    GraphEndpoint = "https://graph.ppe.windows.net/",
                    AuthenticationEndpoint = "https://login.windows-ppe.net/",
                    Name = "PPE",
                    ManagementEndpoint = "https://management.core.windows.net/",
                    ResourceManagerEndpoint = "https://api-dogfood.resources.windows-int.net/",
                    StorageEndpointSuffix = "core.windows.net",
                    KeyVaultSuffix = "vault-int.azure-int.net"
                },
                _ => AzureEnvironment.FromName(name) ?? throw new InvalidDataException("Unknown azure cloud name."),
            };
    }
}
