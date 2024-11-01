using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Identity;
using Azure.SignalRBench.Coordinator.Entities;
using Azure.SignalRBench.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Portal.Entity;

namespace Portal
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }
        

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole(options =>
                    {
                        options.IncludeScopes = true;
                        options.DisableColors = false;
                        options.TimestampFormat = "hh:mm:ss yyyy/MM/dd";
                    });
                })
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
        }
    }
}