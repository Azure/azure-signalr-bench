using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Rpc.Service;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
            => typeof(CommandLineOptions).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        protected override Task OnExecuteAsync(CommandLineApplication app)
        {
            app.ShowHelp();
            return Task.CompletedTask;
        }
    }

    [Command(Name = "agent", FullName = "agent", Description = "Agent command options")]
    internal class AgentCommandOptions : BaseOption
    {
        [Option("-p|--port", Description = "Port to use [7000]. Default is 7000")]
        public int Port { get; } = 7000;

        [Option("--host", Description = "IP to bind. Default is localhost")]
        public string HostName { get; } = "localhost";

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
            var logTarget = RpcLogTargetEnum.All;
            var config = new RpcConfig()
            {
                PidFile = "agent-pid.txt",
                LogTarget = logTarget,
                LogName = "agent-.log",
                LogDirectory = ".",
                RpcPort = option.Port,
                HostName = option.HostName
            };
            return config;
        }
    }

    [Command(Name = "controller", FullName = "controller", Description = "Controller command options")]
    internal class ControllerCommandOptions : BaseOption
    {
        [Option("-a|--agentlist", Description = "Specify the agents endpoint list with ',' as separator. Default is 'localhost:7000'")]
        public IList<string> AgentList { get; } = new[] { "localhost:7000" };

        [Option("-c|--configuration", Description = "Sepcify the configuration filename. If it is '?', it will print help for how to create the configuration YAML file.")]
        public string PluginConfiguration { get; set; }

        protected override async Task OnExecuteAsync(CommandLineApplication app)
        {
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
        }

        private static RpcConfig GenControllerConfig(ControllerCommandOptions option)
        {
            var logTarget = RpcLogTargetEnum.All;

            var config = new RpcConfig()
            {
                PidFile = "master-pid.txt",
                LogTarget = logTarget,
                LogName = "master-.log",
                LogDirectory = ".",
                AgentList = option.AgentList,
                PluginFullName = "Plugin.Microsoft.Azure.SignalR.Benchmark.SignalRBenchmarkPlugin, Plugin.Microsoft.Azure.SignalR.Benchmark",
                PluginConfiguration = option.PluginConfiguration
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

        [Option("-t|--signalr-type", Description = "SignalR type: 0 or 1. 0 for local selfhost SignalR, 1 for Azure SignalR Service, default it is 0")]
        [Range(0, 1, ErrorMessage = "Invalid SignalR type. it must be 0 or 1 for local selfhost SignalR or SignalR Service respectively")]
        public int SignalRType { get; } = 0;

        [Option("-c|--server-connection", Description = "Set the server connection count to ASRS, default is 5")]
        [Range(0, 1024, ErrorMessage = "Current valid connection count is 0 ~ 1024")]
        public int ServerConnectionNumber { get; } = 5;

        [Option("--connection-string", Description = "ASRS connection string")]
        public string ConnectionString { get; }

        [Option("-l|--access-token-lifetime", Description = "Access token's life time with minimum unit is hour, default is 24 hours")]
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
