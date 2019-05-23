using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Azure.SignalR.PerfTest.AppServer;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.Internals.AppServer
{
    public class LocalhostAppServer
    {
        private IWebHost _host;
        private IApplicationLifetime _lifetime;
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
            _host = new WebHostBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.AddSerilog();
                })
                .UseKestrel(KestrelConfig)
                .UseStartup(typeof(Startup))
                .Build();
        }

        public static readonly Action<WebHostBuilderContext, KestrelServerOptions> KestrelConfig =
            (context, options) =>
            {
                options.ListenLocalhost(SignalRConstants.NegoatiationServerPort);
            };

        public async Task Start()
        {
            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
                {
                    Log.Information("Starting localhost app server...");
                    await _host.StartAsync(cts.Token);
                    Log.Information("Localhost app server started");
                    _lifetime = _host.Services.GetRequiredService<IApplicationLifetime>();
                    _started = true;
                    _lifetime.ApplicationStopped.Register(() =>
                    {
                        Log.Information("Negotiation server stopped");
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
