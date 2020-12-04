using Common;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Plugin.Microsoft.Azure.SignalR.Benchmark;
using Rpc.Service;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Azure.SignalR.PerfTest.AppServer
{
    [Command(
        Name = "dotnet signalr-bench",
        FullName = "dotnet-signalr-bench",
        Description = "SignalR benchmark tool")]
    [VersionOptionFromMember(MemberName = nameof(GetVersion))]
    [HelpOption("--help")]
    [Subcommand(
        typeof(AgentCommandOptions),
        typeof(ApplicationCommandOptions),
        typeof(ControllerCommandOptions))]
    internal class CommandLineOptions : BaseOption
    {
        public string GetVersion()
            => typeof(CommandLineOptions).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        protected override Task OnExecuteAsync(CommandLineApplication app)
        {
            app.ShowHelp();
            return Task.CompletedTask;
        }
    }

    [Command(Name = "agent", FullName = "agent", Description = "Agent command options")]
    internal class AgentCommandOptions : BaseOption
    {
        [Option("-p|--port", Description = "Port to use [8099]. Default is 8099")]
        [Range(1024, 65535, ErrorMessage = "Invalid port. Ports must be in the range of 1024 to 65535.")]
        public int Port { get; } = 8099;

        [Option(Description = "Show more console output.")]
        public bool Verbose { get; }

        protected override async Task OnExecuteAsync(CommandLineApplication app)
        {
            try
            {
                var config = GenAgentConfig(this);
                var agent = new Rpc.Agent.Agent(config);
                await agent.Start();
            }
            catch (Exception ex)
            {
                ReportError(ex);
            }
        }

        private static RpcConfig GenAgentConfig(AgentCommandOptions option)
        {
            var logTarget = RpcLogTargetEnum.File;
            if (option.Verbose)
            {
                logTarget = RpcLogTargetEnum.All;
            }
            var config = new RpcConfig()
            {
                PidFile = "agent-pid.txt",
                LogTarget = logTarget,
                LogName = "agent-.log",
                LogDirectory = ".",
                RpcPort = option.Port,
                HostName = "0.0.0.0" // binding for external access
            };
            return config;
        }
    }

    [Command(Name = "controller", FullName = "controller", Description = "Controller command options")]
    internal class ControllerCommandOptions : BaseOption
    {
        [Option("-a|--agentlist",
            Description = "Specify the agents endpoint list with ',' as separator. for example, '10.172.1.5:8099,10.172.1.6:8099', Default is 'localhost:8099'")]
        public string AgentList { get; } = "localhost:8099";

        [Option("-c|--configuration",
            Description = "Specify the configuration YAML filename. If it is '?', it will print help for all options of the configuration.")]
        public string PluginConfiguration { get; set; }

        [Option("-w|--webapp",
            Description = "Specify the SignalR web app hub URL. This option is ignored if '--configuration' is specified. Default value is 'http://localhost:5050/signalrbench'")]
        public string WebAppUrl { get; set; } = SimpleBenchmarkModel.DEFAULT_WEBAPP_HUB_URL;

        [Option("-C|--connections",
            Description = "Specify the client connection counts. This option is ignored if '--configuration' is specified. Default value is 1000.")]
        public uint Connections { get; set; } = SimpleBenchmarkModel.DEFAULT_CONNECTIONS;

        [Option("-b|--basesending",
            Description = "Specify the base sending counts. This option is ignored if '--configuration' is specified. Default value is 500.")]
        public uint BaseSending { get; set; } = SimpleBenchmarkModel.DEFAULT_BASE_SENDING_STEP;

        [Option("-s|--step",
            Description = "Specify the sending number for every step. Typically, the sending message count is gradually increasing to avoid CPU spike. This option is ignored if '--configuration' is specified. Default value is 500.")]
        public uint Step { get; set; } = SimpleBenchmarkModel.DEFAULT_STEP;

        [Option("-d|--duration",
            Description = "The duration (milliseconds) of running for a single step. This option is ignored if '--configuration' is specified. Default value is 240000.")]
        public uint Duration { get; set; } = SimpleBenchmarkModel.DEFAULT_SINGLE_STEP_DUR;

        [Option("-S|--scenario",
            Description = "Specify the scenario: echo|broadcast. If you want to try more scenarios, please use --configuration. This option is ignored if '--configuration' is specified. Default value is echo.")]
        public string Scenario { get; set; } = SimpleBenchmarkModel.DEFAULT_SCENARIO;

        [Option("-D|--debug",
            Description = "Enable debug for printing more details. This option is ignored if '--configuration' is specified. Default value is true.")]
        public string Debug { get; set; } = "true";

        [Option("-u|--use-builtin-agent",
            Description = "Launch builtin agent for a quick start if you specify 1. It is not recommended for an official perf test. Default is 0.")]
        [Range(0, 1, ErrorMessage = "Valid input is [0,1]")]
        public int UseBuiltinAgent { get; set; } = 0;

        private Process LaunchAgent()
        {
            var process = new Process();
            if (System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName == "dotnet.exe")
            {
                var dotnetPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                var cmdLines = Environment.GetCommandLineArgs();
                var dllPath = cmdLines[0];
                process.StartInfo = new ProcessStartInfo(dotnetPath, $"exec {cmdLines[0]} agent");
                Console.WriteLine($"{dotnetPath} exec {cmdLines[0]} agent");
            }
            else
            {
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                process.StartInfo = new ProcessStartInfo(exePath, "agent");
            }
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
            Console.WriteLine($"working dir: {process.StartInfo.WorkingDirectory}");
            return process;
        }

        protected override async Task OnExecuteAsync(CommandLineApplication app)
        {
            Process p = null;
            if (UseBuiltinAgent == 1)
            {
                p = LaunchAgent();
                if (p.Start())
                {
                    Console.WriteLine("agent started");
                }
                else
                {
                    Console.WriteLine("agent failed");
                }
            }
            try
            {
                var config = GenControllerConfig(this);
                var controller = new Rpc.Master.Controller(config);
                await controller.Start();
            }
            catch (Exception ex)
            {
                ReportError(ex);
            }
            finally
            {
                if (p != null)
                {
                    p.Kill();
                    Console.WriteLine("agent stopped");
                }
            }
        }

        private static RpcConfig GenControllerConfig(ControllerCommandOptions option)
        {
            var logTarget = RpcLogTargetEnum.All;
            string configureFile = option.PluginConfiguration;
            if (option.PluginConfiguration == null)
            {
                // compose the YAML configuration from parameters
                configureFile = $"{option.Scenario}-{Util.Timestamp()}.yaml";
                string configureContent = $@"
mode: simple      # Required: 'Simple|Advanced', default is 'Simple'
config:
  webAppTarget: {option.WebAppUrl}
  connections: {option.Connections}
  baseSending: {option.BaseSending}
  step: {option.Step}
  singleStepDuration: {option.Duration}
  debug: {option.Debug}
scenario:
  name: {option.Scenario}
";
                using (StreamWriter sw = new StreamWriter(configureFile, true))
                {
                    sw.Write(configureContent);
                }
                option.PluginConfiguration = configureFile;
            }
            var config = new RpcConfig()
            {
                PidFile = "controller-pid.txt",
                LogTarget = logTarget,
                LogName = "controller-.log",
                LogDirectory = ".",
                AgentList = option.AgentList.Split(','),
                PluginFullName = "Plugin.Microsoft.Azure.SignalR.Benchmark.SignalRBenchmarkPlugin, Plugin.Microsoft.Azure.SignalR.Benchmark",
                PluginConfiguration = configureFile
            };

            return config;
        }
    }

    [Command(Name = "application", FullName = "application", Description = "Application command options")]
    internal class ApplicationCommandOptions : BaseOption
    {
        private LogLevel? _logLevel;

        [Option(Description = "Port to use [5050]. Use 0 for a dynamic port.")]
        [Range(1024, 65535, ErrorMessage = "Invalid port. Ports must be in the range of 1024 to 65535.")]
        public int Port { get; } = 5050;

        [Option(Description = "Show less console output.")]
        public bool Quiet { get; }

        [Option("-t|--signalr-type", Description = "SignalR type: 0 or 1. 0 for local selfhost SignalR, 1 for Azure SignalR Service, default it is 0.")]
        [Range(0, 1, ErrorMessage = "Invalid SignalR type. it must be 0 or 1 for local selfhost SignalR or SignalR Service respectively")]
        public int SignalRType { get; } = 0;

        [Option("-c|--server-connection", Description = "Set the server connection count to ASRS, default is 5.")]
        [Range(0, 1024, ErrorMessage = "Current valid connection count is 0 ~ 1024")]
        public int ServerConnectionNumber { get; } = 5;

        [Option("-s|--connection-string", Description = "Specify the ASRS connection string.")]
        public string ConnectionString { get; }

        [Option("-l|--access-token-lifetime", Description = "Specify access token's life time in hour, default is 24 hours.")]
        [Range(1, 1440, ErrorMessage = "Valid input is 1 ~ 1440")]
        public int AccessTokenLifetime { get; } = 24;

        [Option(Description = "Show more console output.")]
        public bool Verbose { get; }

        [Option("--log <LEVEL>", Description = "For advanced diagnostics.", ShowInHelpText = false)]
        public LogLevel MinLogLevel
        {
            get
            {
                if (_logLevel.HasValue)
                {
                    return _logLevel.Value;
                }

                if (Quiet)
                {
                    return LogLevel.Error;
                }

                if (Verbose)
                {
                    return LogLevel.Debug;
                }

                return LogLevel.Information;
            }
            private set => _logLevel = value;
        }

        protected override async Task OnExecuteAsync(CommandLineApplication app)
        {
            try
            {
                var server = new AppServer(this, PhysicalConsole.Singleton);
                await server.RunAsync();
            }
            catch (Exception ex)
            {
                ReportError(ex);
            }
        }
    }

    [HelpOption("--help")]
    internal abstract class BaseOption
    {
        protected virtual Task OnExecuteAsync(CommandLineApplication app)
        {
            return Task.CompletedTask;
        }

        protected static void ReportError(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Unexpected error: {ex}");
            Console.ResetColor();
        }
    }

}
