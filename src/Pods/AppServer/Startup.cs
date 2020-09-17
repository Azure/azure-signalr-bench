// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Azure.SignalRBench.AppServer
{
    public class Startup
    {
        internal const string HUB_NAME = "/signalrbench";
        private const string ASRSConnectionStringKey = "SignalR:ConnectionString";
        private const string ASRSConnectionNumberKey = "SignalR:ConnectionNumber";

        private readonly ILogger<Startup> _logger;

        public Startup(IConfiguration configuration, ILogger<Startup> logger)
        {
            Configuration = configuration;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            services.AddSingleton<MessageClientHolder>();
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
    }
}
