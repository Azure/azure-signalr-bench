using McMaster.Extensions.CommandLineUtils;
using Rpc.Service;
using System;

namespace Microsoft.Azure.SignalR.PerfTest.AppServer
{
    class Program
    {
        private const int ERROR = 2;
        private const int OK = 0;

        static int Main(string[] args)
        {
            try
            {
                var app = new CommandLineApplication<CommandLineOptions>();
                app.Command<AgentCommandOptions>("agent", cmd =>
                {
                    cmd.OnExecute(async () =>
                    {
                        try
                        {
                            var config = GenAgentConfig(cmd.Model);
                            var agent = new Rpc.Agent.Agent(config);
                            await agent.Start();
                        }
                        catch (Exception ex)
                        {
                            ReportError(ex);
                        }
                    });
                });
                app.Command<ControllerCommandOptions>("controller", cmd =>
                {
                    cmd.OnExecute(async () =>
                    {
                        var config = GenControllerConfig(cmd.Model);
                        var controller = new Rpc.Master.Controller(config);
                        await controller.Start();
                    });
                });
                app.Command<ApplicationCommandOptions>("application", cmd =>
                {
                    cmd.OnExecute(async () =>
                    {
                        try
                        {
                            var server = new AppServer(cmd.Model, PhysicalConsole.Singleton);
                            await server.RunAsync();
                        }
                        catch (Exception ex)
                        {
                            ReportError(ex);
                        }
                    });
                });
                app.Conventions.UseDefaultConventions();
                app.OnExecute(() =>
                {
                    Console.WriteLine("Specify a subcommand");
                    app.ShowHelp();
                    return 1;
                });
                return app.Execute(args);
            }
            catch (Exception ex)
            {
                ReportError(ex);
                return ERROR;
            }
        }

        private static void ReportError(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Unexpected error: {ex}");
            Console.ResetColor();
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
}
