// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Threading.Tasks;
using Azure.Messaging.WebPubSub;
using Azure.SignalRBench.Client.ClientAgentFactory;
using Azure.SignalRBench.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Azure.SignalRBench.Client
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
                    logging.AddProvider(
                        new BlobLoggerProvider(
                            $"{context.Configuration[PerfConstants.ConfigurationKeys.TestIdKey]}/{Roles.Clients}_{context.Configuration[PerfConstants.ConfigurationKeys.PodNameStringKey]}",
                            ".log",
                            context.Configuration[PerfConstants.ConfigurationKeys.StorageConnectionStringKey]));
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<MessageClientHolder>();
                    services.AddSingleton<IScenarioState, ScenarioState>();
                    switch (hostContext.Configuration[PerfConstants.ConfigurationKeys.TestCategory])
                    {
                        case nameof(TestCategory.AspnetCoreSignalR):
                            services.AddSingleton<IClientAgentFactory, SignalRDefaultClientAgentFactory>();
                            break;
                        case nameof(TestCategory.AspnetSignalR):
                            services.AddSingleton<IClientAgentFactory, AspNetSignalRClientAgentFactory>();
                            Console.WriteLine("aspnet");
                            break;
                        case nameof(TestCategory.AspnetCoreSignalRServerless):
                            services.AddSingleton<IClientAgentFactory, SignalRServerlessClientAgentFactory>();
                            break;
                        case nameof(TestCategory.RawWebsocket):
                            services.AddSingleton<IClientAgentFactory, WebsocketClientAgentFactory>();
                            break;
                        case nameof(TestCategory.SocketIO):
                            services.AddSingleton<IClientAgentFactory, SioClientAgentFactory>();
                            break;
                        default:
                            Console.WriteLine(
                                $"Unknown testCategory:{hostContext.Configuration[PerfConstants.ConfigurationKeys.TestCategory]}");
                            break;
                    }

                    services.AddHostedService<ClientHostedService>();
                });
        }
    }
}