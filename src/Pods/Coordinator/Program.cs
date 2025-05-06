// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Coordinator.Provider;
using Azure.SignalRBench.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Azure.SignalRBench.Coordinator
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureLogging((context, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConsole(options =>
                    {
                        options.IncludeScopes = true;
                        options.DisableColors = false;
                        options.TimestampFormat = "hh:mm:ss yyyy/MM/dd";
                    });
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton(
                        sp => new SecretClient(
                            new Uri(hostContext.Configuration[PerfConstants.ConfigurationKeys.KeyVaultUrlKey]),
                            new DefaultAzureCredential(new DefaultAzureCredentialOptions
                            {
                                ManagedIdentityClientId =
                                    hostContext.Configuration[PerfConstants.ConfigurationKeys.MsiAppId]
                            })));
                    services.AddSingleton<IPerfStorage>(sp =>
                        {
                            var secretClient = sp.GetService<SecretClient>();
                            var queueUrl = secretClient.GetSecretAsync(PerfConstants.KeyVaultKeys.StorageQueueUrlKey).GetAwaiter().GetResult()
                                .Value.Value;
                            var cdbUrl = secretClient.GetSecretAsync(PerfConstants.KeyVaultKeys.CosmosUrlKey).GetAwaiter().GetResult()
                                .Value.Value;
                            var blobUrl = secretClient.GetSecretAsync(PerfConstants.KeyVaultKeys.StorageBlobUrlKey).GetAwaiter().GetResult()
                                .Value.Value;
                            return new PerfStorage(queueUrl,cdbUrl,blobUrl,hostContext.Configuration[PerfConstants.ConfigurationKeys.MsiAppId]);
                        }
                    );
                    services.AddSingleton<PerfStorageProvider>();
                    services.AddSingleton<IK8sProvider>(sp =>
                    {
                        var secretClient = sp.GetService<SecretClient>();
                        var enableDockerImageStr = secretClient.GetSecretAsync(PerfConstants.KeyVaultKeys.EnableDockerImage).GetAwaiter().GetResult()
                            .Value.Value;
                        var enableDockerImage = bool.TryParse(enableDockerImageStr, out var result) && result;
                        var perfStorage = sp.GetService<IPerfStorage>();
                        if (enableDockerImage)
                        {
                            return new K8SProviderDocker(perfStorage, hostContext.Configuration, secretClient);
                        }
                        else
                        {
                           return new K8SProvider(perfStorage, hostContext.Configuration); 
                        }
                    });
                    services.AddSingleton<IAksProvider, AksProvider>();
                    services.AddSingleton<SignalRProvider>();
                    services.AddSingleton<TestScheduler>();
                    services.AddSingleton<TestRunnerFactory>();
                    services.AddSingleton<TimeCoordinator>();
                    services.AddHostedService<CoordinatorHostedService>();
                });
        }
    }
}