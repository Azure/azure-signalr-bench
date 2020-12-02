// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.SignalRBench.Common;
using Microsoft.AspNetCore.Hosting;
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
            return Host.CreateDefaultBuilder(args)
              .ConfigureLogging((context, logging) =>
              {
                  logging.ClearProviders();
                  logging.AddConsole();
                  logging.AddProvider(
                      new BlobLoggerProvider(
                          $"{context.Configuration[Constant.ConfigurationKeys.TestIdKey]}/{Roles.AppServers}_{context.Configuration[Constant.ConfigurationKeys.PodNameStringKey]}",
                          ".log",
                          context.Configuration[Constant.ConfigurationKeys.StorageConnectionStringKey]));
              })
              .ConfigureWebHostDefaults(webBuilder =>
              {
                  webBuilder.UseStartup<Startup>();
              });
        }
    }
}
