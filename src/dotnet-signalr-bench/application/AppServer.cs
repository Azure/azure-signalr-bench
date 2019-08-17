using McMaster.Extensions.CommandLineUtils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.SignalR.PerfTest.AppServer
{
    public class AppServer
    {
        private readonly ApplicationCommandOptions _options;
        private readonly IConsole _console;
        private readonly IReporter _reporter;
        
        internal AppServer(ApplicationCommandOptions options, IConsole console)
        {
            _options = options;
            _console = console;
            _reporter = new ConsoleReporter(console)
            {
                IsQuiet = options.Quiet,
                IsVerbose = options.Verbose,
            };
            ValidateCommandLineOptions(options, _reporter);
        }

        public async Task<int> RunAsync()
        {
            var port = _options.Port;
            var cts = new CancellationTokenSource();
            _console.CancelKeyPress += (o, e) =>
            {
                _console.WriteLine("Shutting down...");
                cts.Cancel();
            };

            var config = GetAppServerConfig(_options);
            var host = new WebHostBuilder()
                .ConfigureLogging(l =>
                {
                    l.SetMinimumLevel(_options.MinLogLevel);
                    l.AddConsole();
                })
                .PreferHostingUrls(false)
                .UseKestrel(o =>
                {
                    o.ListenAnyIP(port);
                })
                .UseEnvironment("Production")
                .UseStartup<Startup>()
                .ConfigureServices(s => s.AddSingleton(config))
                .Build();

            _console.Write("Starting server");
            await host.StartAsync(cts.Token);
            AfterServerStart(host);
            await host.WaitForShutdownAsync(cts.Token);
            return 0;
        }

        private void ValidateCommandLineOptions(ApplicationCommandOptions options, IReporter reporter)
        {
            if (options.SignalRType == 1 && string.IsNullOrEmpty(options.ConnectionString))
            {
                var err = "Use ASRS but forget to set connection string!";
                reporter.Error(err);
                throw new InvalidOperationException(err);
            }
        }

        private AppServerConfig GetAppServerConfig(ApplicationCommandOptions option)
        {
            var config = new AppServerConfig()
            {
                SignalRType = option.SignalRType,
                AccessTokenLifetime = option.AccessTokenLifetime,
                ConnectionNumber = option.ServerConnectionNumber,
                ConnectionString = option.ConnectionString,
                MinLogLevel = option.MinLogLevel
            };
            return config;
        }

        private void AfterServerStart(IWebHost host)
        {
            var addresses = host.ServerFeatures.Get<IServerAddressesFeature>();
            //var pathBase = _options.GetPathBase();

            _console.WriteLine("Listening on:");
            foreach (var a in addresses.Addresses)
            {
                _console.WriteLine($"  {a}");
            }

            _console.WriteLine("");
            _console.WriteLine("Press CTRL+C to exit");
        }
    }
}
