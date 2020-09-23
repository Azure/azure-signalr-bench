// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.SignalRBench.Common;
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

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                 .ConfigureLogging((ILoggingBuilder logging) =>
                 {
                     logging.ClearProviders();
                     logging.AddConsole();
                 })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton(
                        sp => new SecretClient(
                            new Uri(hostContext.Configuration[Constants.ConfigurationKeys.KeyVaultUrlKey]),
                            new DefaultAzureCredential()));
                    services.AddSingleton<PerfStorageProvider>();
                    services.AddSingleton<K8sProvider>();
                    services.AddSingleton<AksProvider>();
                    services.AddSingleton<ArmProvider>();
                    services.AddSingleton<SignalRProvider>();
                    services.AddSingleton<TestScheduler>();
                    services.AddHostedService<CoordinatorHostedService>();
                });
    }
}