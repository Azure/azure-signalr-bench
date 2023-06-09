// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Azure.SignalRBench.AppServer.Hub;
using Azure.SignalRBench.AppServer.Router;
using Azure.SignalRBench.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Azure.SignalRBench.AppServer
{
    public class Startup
    {
        internal const string HUB_NAME = "/signalrbench";

        //  private readonly ILogger<Startup> _logger;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            //   _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var fp = Configuration[PerfConstants.ConfigurationKeys.Protocol];
            ISignalRServerBuilder builder = null;
            if (!fp.ToLower().Contains("json"))
            {
                builder = services.AddSignalR(options =>
                {
                    var supportedProtocol = new List<string> {"messagepack"};
                    options.SupportedProtocols = supportedProtocol;
                    options.MaximumReceiveMessageSize = 1024 * 1024 * 100;
                }).AddMessagePackProtocol();
            }
            else
            {
                builder = services.AddSignalR(options => options.MaximumReceiveMessageSize = 1024 * 1024 * 100
                );
            }

            services.AddSingleton(typeof(IEndpointRouter), typeof(ReplicaRouter));
            builder.AddAzureSignalR(option =>
            {
                option.ConnectionCount = Configuration[PerfConstants.ConfigurationKeys.ConnectionNum] != null
                    ? Configuration.GetValue<int>(PerfConstants.ConfigurationKeys.ConnectionNum)
                    : 50;
                var connectString = Configuration[PerfConstants.ConfigurationKeys.ConnectionString];
                // multiple endpoint
                var endpoints = connectString.Split(" ");
                if (endpoints.Length <= 1)
                {
                    option.ConnectionString = Configuration[PerfConstants.ConfigurationKeys.ConnectionString];
                }
                else
                {
                    option.Endpoints = endpoints.Select(e => new ServiceEndpoint(e)).ToArray();
                }
            });
            services.AddSingleton<MessageClientHolder>();
            services.AddHostedService<ServerHostedService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();

            app.UseAzureSignalR(routes => { routes.MapHub<BenchHub>(HUB_NAME); });
        }
    }
}