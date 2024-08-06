// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Threading.Tasks;
using Azure.Messaging.WebPubSub;
using Azure.SignalRBench.Client.ClientAgentFactory;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;

namespace Azure.SignalRBench.Client
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            using var server = new Prometheus.KestrelMetricServer(port: 8080);
            server.Start();
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
                    var testId = hostContext.Configuration[PerfConstants.ConfigurationKeys.TestIdKey];
                    var tmp = testId.Split("--");
                    ClientMetrics.Init(tmp[0],tmp[1]);
                    services.AddSingleton<MessageClientHolder>();
                    services.AddSingleton<IScenarioState, ScenarioState>();
                    services.AddSingleton<IPerfStorage>(sp =>
                    {
                        var saConnectionString = hostContext.Configuration[PerfConstants.ConfigurationKeys.StorageConnectionStringKey];
                        var cdbConnectionString = hostContext.Configuration[PerfConstants.ConfigurationKeys.CosmosConnectionStringKey];
                        return new PerfStorage(saConnectionString, cdbConnectionString);
                    });
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
                        case nameof(TestCategory.WebPubSubPythonSdk):
                            services.AddSingleton<IClientAgentFactory, WebsocketPythonServerClientAgentFactory>();
                            break;
                        case nameof(TestCategory.Mqtt):
                            services.AddSingleton<IClientAgentFactory, MqttClientAgentFactory>();
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