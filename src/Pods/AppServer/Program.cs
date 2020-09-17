// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.SignalRBench.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Azure.SignalRBench.AppServer
{
    public class Program
    {
        public static void Main(string[] args)
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
                          $"{configuration[Constants.EnvVariableKey.TestIdKey]}/{Roles.AppServers}_{configuration[Constants.EnvVariableKey.PodNameStringKey]}",
                          ".log",
                          configuration[Constants.EnvVariableKey.StorageConnectionStringKey]));
              })
              .ConfigureWebHostDefaults(webBuilder =>
              {
                  webBuilder.UseStartup<Startup>();
              });
        }
    }
}
