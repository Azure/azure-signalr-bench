// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.SignalRBench.Storage;
using Coordinator.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace Coordinator
{
    internal class Program
    {
        static void Main(string[] args)
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
                    var config = hostContext.Configuration;
                    string kvUrl = config["kvUrl"];
                    var secretClient = new SecretClient(new Uri(kvUrl), new DefaultAzureCredential());
                    PerfConfig.Init(secretClient);
                    services.AddSingleton(sp => secretClient);
                    services.AddSingleton(sp => new PerfStorageProvider(kvUrl, null));
                    services.AddSingleton<KubeCtlHelper>();
                    services.AddSingleton<AksHelper>();
                    services.AddSingleton<SignalRHelper>();
                    services.AddHostedService<Worker>();
                });
    }
}