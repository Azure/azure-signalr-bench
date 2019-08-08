using McMaster.Extensions.CommandLineUtils;
using Microsoft.AspNetCore.Hosting;
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
        
        public AppServer(ApplicationCommandOptions options, IConsole console)
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
            var cts = new CancellationTokenSource();
            var port = _options.Port;

            _console.CancelKeyPress += (o, e) =>
            {
                _console.WriteLine("Shutting down...");
                cts.Cancel();
            };
            
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
                .ConfigureServices(s => s.AddSingleton(_options))
                .Build();

            _console.Write("Starting server");
            await host.StartAsync(cts.Token);
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

    }
}
