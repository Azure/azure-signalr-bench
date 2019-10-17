using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.Internals
{
    public class NegotiationServer
    {
        private static int DEFAULT_LOCAL_PORT = 12345;
        private IWebHost _host;
        private IHostApplicationLifetime _lifetime;
        private bool _started;
        private IServiceManager _serviceManager;

        public bool IsStarted {
            get
            {
                return _started;
            }
        }

        public NegotiationServer(string connectionString)
        {
            _serviceManager = new ServiceManagerBuilder()
                .WithOptions(o => o.ConnectionString = connectionString)
                .Build();
            _host = new WebHostBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                .ConfigureServices(services => services.Add(ServiceDescriptor.Singleton(_serviceManager)))
                .UseKestrel(KestrelConfig)
                .UseStartup(typeof(InternalStartup))
                .Build();
        }

        public async Task Start()
        {
            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
                {
                    Log.Information("Starting negotiation server...");
                    await _host.StartAsync(cts.Token);
                    Log.Information("Negotiation server started");
                    _lifetime = _host.Services.GetRequiredService<IHostApplicationLifetime>();
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

        public static readonly Action<WebHostBuilderContext, KestrelServerOptions> KestrelConfig =
            (context, options) =>
            {
                options.ListenLocalhost(DEFAULT_LOCAL_PORT);
            };
    }
}
