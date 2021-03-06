﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Azure.SignalR.PerfTest.AppServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.Internals.AppServer
{
    public class LocalhostAppServer
    {
        private IWebHost _host;
        private IHostApplicationLifetime _lifetime;
        private bool _started;

        public bool IsStarted
        {
            get
            {
                return _started;
            }
        }

        public LocalhostAppServer(string connectionString, int port = SignalRConstants.LocalhostAppServerPort)
        {
            bool useLocalSignalR = false;
            var useLocalSignalRValue = Environment.GetEnvironmentVariable("useLocalSignalR");
            if (!string.IsNullOrEmpty(useLocalSignalRValue))
            {
                if (Boolean.TryParse(useLocalSignalRValue, out var result))
                {
                    useLocalSignalR = result;
                }
            }
            var config = new AppServerConfig()
            {
                SignalRType = useLocalSignalR ? 0 : 1,
                ConnectionString = connectionString
            };
            _host = new WebHostBuilder()
                .ConfigureLogging((context, logging) =>
                {
                    logging.AddSerilog();
                    logging.SetMinimumLevel(LogLevel.Warning);
                })
                .ConfigureAppConfiguration(ConfigurationConfig)
                .ConfigureServices(s => s.AddSingleton(config))
                .UseKestrel(KestrelConfig)
                .UseStartup(typeof(Startup))
                .Build();
        }

        public static readonly Action<WebHostBuilderContext, IConfigurationBuilder> ConfigurationConfig =
            (context, builder) =>
            {
                builder.SetBasePath(context.HostingEnvironment.ContentRootPath)
                       .AddEnvironmentVariables();
            };

        public static readonly Action<WebHostBuilderContext, KestrelServerOptions> KestrelConfig =
            (context, options) =>
            {
                options.ListenLocalhost(SignalRConstants.LocalhostAppServerPort);
            };

        public async Task Start()
        {
            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
                {
                    Log.Information("Starting localhost app server...");
                    await _host.StartAsync(cts.Token);
                    var url = _host.ServerFeatures.Get<IServerAddressesFeature>().Addresses.FirstOrDefault();
                    Log.Information($"Localhost app server started {url}");
                    _lifetime = _host.Services.GetRequiredService<IHostApplicationLifetime>();
                    _started = true;
                    _lifetime.ApplicationStopped.Register(() =>
                    {
                        Log.Information("Localhost app server stopped");
                        _started = false;
                    });
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error in start negotiation server: {e.Message}");
            }
        }

        public async Task Stop()
        {
            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
                {
                    await _host.StopAsync(cts.Token);
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error in stop negotiation server: {e.Message}");
            }
        }
    }
}
