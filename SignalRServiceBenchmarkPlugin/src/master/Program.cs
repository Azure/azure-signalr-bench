using CommandLine;
using Common;
using Rpc.Service;
using Serilog;
using System.Threading.Tasks;

namespace Rpc.Master
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Parse args
            var argsOption = ParseArgs(args);

            if (argsOption == null)
            {
                return;
            }
            var config = GenConfig(argsOption);
            var controller = new Controller(config);
            await controller.Start();
        }

        private static RpcConfig GenConfig(ArgsOption option)
        {
            var logTarget = RpcLogTargetEnum.All;
            if (option.LogTarget == LogTargetEnum.Console)
            {
                logTarget = RpcLogTargetEnum.Console;
            }
            else if (option.LogTarget == LogTargetEnum.File)
            {
                logTarget = RpcLogTargetEnum.File;
            }
            var config = new RpcConfig()
            {
                PidFile = option.PidFile,
                LogTarget = logTarget,
                LogName = option.LogName,
                LogDirectory = option.LogDirectory,
                AgentList = option.AgentList,
                PluginFullName = option.PluginFullName,
                PluginConfiguration = option.BenchmarkConfiguration
            };

            return config;
        }

        private static ArgsOption ParseArgs(string[] args)
        {
            Log.Information($"Parse arguments...");
            var argsOption = new ArgsOption();
            var result = Parser.Default.ParseArguments<ArgsOption>(args)
                .WithParsed(options => argsOption = options)
                .WithNotParsed(error => 
                {
                    argsOption = null;
                });
            return argsOption;
        }
    }
}
