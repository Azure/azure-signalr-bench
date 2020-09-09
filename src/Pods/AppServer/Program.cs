using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Azure.SignalRBench.AppServer
{
    public class Program
    {
        private const string StorageConnectionStringKey = "Storage:SignalR:ConnectionString";
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
                  logging.AddProvider(new BlobLoggerProvider(configuration[StorageConnectionStringKey], "appserver", ""));
              })
              .ConfigureWebHostDefaults(webBuilder =>
              {
                  webBuilder.UseStartup<Startup>();
              });
        }
    }
}
