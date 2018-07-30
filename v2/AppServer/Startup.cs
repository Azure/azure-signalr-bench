// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.SignalR.PerfTest.AppServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            useLocalSignalR = Environment.GetEnvironmentVariable("useLocalSignalR") == null || Environment.GetEnvironmentVariable("useLocalSignalR") == "" || Environment.GetEnvironmentVariable("useLocalSignalR") == "false" ? false : true;
            Console.BackgroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine($"use local signalr: {useLocalSignalR}");
            Console.BackgroundColor = ConsoleColor.Black;
        }

        public IConfiguration Configuration { get; }
        private bool useLocalSignalR = false;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            if (useLocalSignalR)
                services.AddSignalR().AddMessagePackProtocol();
            else
                services.AddSignalR().AddMessagePackProtocol().AddAzureSignalR(option =>
                {
                    option.ConnectionCount = Configuration.GetValue<int>("Azure:SignalR:ConnectionNumber");
                });
        }

        public void Configure(IApplicationBuilder app)
        {
            // TODO: configure endpoint from file
            app.UseMvc();
            app.UseFileServer();
            if (useLocalSignalR)
                app.UseSignalR(routes =>
                {
                    routes.MapHub<BenchHub>("/signalrbench");
                });
            else
                app.UseAzureSignalR(routes =>
                {
                    routes.MapHub<BenchHub>("/signalrbench");
                });

        }

    }
}
