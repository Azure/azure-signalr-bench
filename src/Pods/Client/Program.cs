// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.SignalRBench.Common;
using Microsoft.Extensions.Configuration;
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
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();
            return Host.CreateDefaultBuilder(args)
                .ConfigureLogging((ILoggingBuilder logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddProvider(
                        new BlobLoggerProvider(
                            $"{configuration[Constants.EnvVariableKey.TestIdKey]}/{Roles.Clients}_{configuration[Constants.EnvVariableKey.PodNameStringKey]}",
                            ".log",
                            configuration[Constants.EnvVariableKey.StorageConnectionStringKey]));
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<MessageClientHolder>();
                    services.AddSingleton<IScenarioState, ScenarioState>();
                });
        }
    }
}
