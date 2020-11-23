// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.SignalR.PerfTest.AppServer
{
    public class Startup
    {
        internal const string HUB_NAME = "/signalrbench";

        private readonly AppServerConfig _serverConfig;
        private readonly bool _useLocalSignalR;

        public Startup(AppServerConfig serverConfig)
        {
            _serverConfig = serverConfig;
            Console.BackgroundColor = ConsoleColor.DarkMagenta;
            _useLocalSignalR = _serverConfig.SignalRType == 0;
            Console.WriteLine($"use local signalr: {_useLocalSignalR}");
            Console.BackgroundColor = ConsoleColor.Black;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            if (_useLocalSignalR)
            {
                services.AddSignalR(o => o.MaximumReceiveMessageSize = null).AddMessagePackProtocol();
            }
            else
            {
                services.AddSignalR(o => o.MaximumReceiveMessageSize = null)
                        .AddMessagePackProtocol()
                        .AddAzureSignalR(option =>
                {
                    option.AccessTokenLifetime = TimeSpan.FromHours(_serverConfig.AccessTokenLifetime);
                    option.ConnectionCount = _serverConfig.ConnectionNumber;
                    option.Endpoints = Array.ConvertAll(_serverConfig.ConnectionString.Split("!"), c => new ServiceEndpoint(c));
                    // option.DiagnosticClientFilter = ctx => true;
                });
            }
            services.Replace(ServiceDescriptor.Singleton(typeof(ILoggerFactory), typeof(TimedLoggerFactory)));
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            if (_useLocalSignalR)
            {
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapHub<BenchHub>(HUB_NAME);
                });
            }
            else
            {
                app.UseAzureSignalR(routes =>
                {
                    routes.MapHub<BenchHub>(HUB_NAME);
                });
            }
        }

    }
}
