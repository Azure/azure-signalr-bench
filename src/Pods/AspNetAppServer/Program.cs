using System;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Microsoft.Owin.Hosting;

namespace AspNetAppServer
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            var url = "http://*:8080";
            using (WebApp.Start<Startup>(url))
            {
                Console.WriteLine($"Server running at {url}");
                await new MessageClientHolder().InitializeAsync(
                    Environment.GetEnvironmentVariable(PerfConstants.ConfigurationKeys.TestIdKey),
                    Environment.GetEnvironmentVariable(PerfConstants.ConfigurationKeys.RedisConnectionStringKey),
                    Environment.GetEnvironmentVariable(PerfConstants.ConfigurationKeys.PodNameStringKey));
                //  Prevent process to exit
                await Task.Delay(-1);
            }
        }
    }
}