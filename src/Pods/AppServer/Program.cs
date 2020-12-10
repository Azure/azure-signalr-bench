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
                  logging.AddConsole(options =>
                  {
                      options.IncludeScopes = true;
                      options.DisableColors = false;
                      options.TimestampFormat = "hh:mm:ss yyyy/MM/dd";
                  });
                  logging.AddProvider(
                      new BlobLoggerProvider(
                          $"{context.Configuration[PerfConstants.ConfigurationKeys.TestIdKey]}/{Roles.AppServers}_{context.Configuration[PerfConstants.ConfigurationKeys.PodNameStringKey]}",
                          ".log",
                          context.Configuration[PerfConstants.ConfigurationKeys.StorageConnectionStringKey]));
              })
              .ConfigureWebHostDefaults(webBuilder =>
              {
                  webBuilder.UseStartup<Startup>();
              });
        }
    }
}
