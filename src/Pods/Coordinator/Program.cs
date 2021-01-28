// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.SignalRBench.Common;
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
                    if (!context.HostingEnvironment.IsDevelopment())
                        logging.AddProvider(
                            new BlobLoggerProvider(
                                $"{Roles.Coordinator}/{Roles.Coordinator}_{context.Configuration[PerfConstants.ConfigurationKeys.PodNameStringKey]}",
                                ".log",
                                context.Configuration[PerfConstants.ConfigurationKeys.StorageConnectionStringKey]));
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
                            var connectionString = secretClient
                                .GetSecretAsync(PerfConstants.KeyVaultKeys.StorageConnectionStringKey).GetAwaiter()
                                .GetResult().Value.Value;
                            return new PerfStorage(connectionString);
                        }
                    );
                    services.AddSingleton<PerfStorageProvider>();
                    services.AddSingleton<IK8sProvider, K8sProvider>();
                    services.AddSingleton<IAksProvider, AksProvider>();
                    services.AddSingleton<SignalRProvider>();
                    services.AddSingleton<TestScheduler>();
                    services.AddSingleton<TestRunnerFactory>();
                    services.AddHostedService<CoordinatorHostedService>();
                });
        }
    }
}