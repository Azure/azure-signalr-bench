// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Azure.SignalRBench.Client
{
    internal class Program
    {
        private static void  Main(string[] args)
        {
            int maxIO , minIO;
            int maxThreads, minThreads;
            int avt, iot;
            ThreadPool.SetMinThreads(200,0);
            ThreadPool.GetMaxThreads(out maxThreads, out maxIO);
            ThreadPool.GetMinThreads(out minThreads, out minIO);
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(1000);
                    ThreadPool.GetAvailableThreads(out avt, out iot);

                    var active = maxThreads - avt;
                    var activeIO = maxIO - iot;
                    var completedItemCount = ThreadPool.CompletedWorkItemCount;
                    var pendingWorkItemCount = ThreadPool.PendingWorkItemCount;
                    var threadCount = ThreadPool.ThreadCount;
                    int number = Process.GetCurrentProcess().Threads.Count;
                    Console.WriteLine($"max:{maxThreads}, min:{minThreads},IOMax:{maxIO},IOMin:{minIO}");
                    Console.WriteLine($"active worker:{active}, acitve IO:{activeIO}");
                    Console.WriteLine($"pending:{pendingWorkItemCount}, completed: {completedItemCount}");
                    Console.WriteLine($"threadCount: {threadCount}");
                    Console.WriteLine($"process thread:{number}");
                }
            });
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