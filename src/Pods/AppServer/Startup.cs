using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Messages;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Azure.SignalRBench.AppServer
{
    public class Startup
    {
        internal const string HUB_NAME = "/signalrbench";
        private const string ASRSConnectionStringKey = "Azure:SignalR:ConnectionString";
        private const string ASRSConnectionNumberKey = "Azure:SignalR:ConnectionNumber";
        private const string RedisConnectionStringKey = "Redis:SignalR:ConnectionString";


        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            AddComandHandlers(configuration[RedisConnectionStringKey]);
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR().AddMessagePackProtocol()
                 .AddAzureSignalR(option =>
                 {
                     option.ConnectionCount = Configuration[ASRSConnectionStringKey] != null ? Configuration.GetValue<int>(ASRSConnectionNumberKey) : 5;
                     option.ConnectionString = Configuration[ASRSConnectionStringKey];
                 });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();

            app.UseAzureSignalR(routes =>
            {
                routes.MapHub<BenchHub>(HUB_NAME);
            });
        }

        private void AddComandHandlers(string connectionString)
        {
            const string sender = "AppServer";
            var crash = MessageHandler.CreateCommandHandler(Commands.General.Crash, cmd =>
               {
                   Environment.Exit(1);
                   return Task.CompletedTask;
               });
            Task.Run(async () => await MessageClient.ConnectAsync(connectionString, sender, crash));
        }
    }
}
