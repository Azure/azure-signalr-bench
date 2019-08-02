using System;
using CommandLine;
using System.Threading.Tasks;
using Rpc.Service;
using Common;
using Serilog;

namespace Rpc.Agent
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            // Parse args
            var argsOption = ParseArgs(args);

            if (argsOption == null)
            {
                return;
            }

            Util.SavePidToFile(argsOption.PidFile);

            // Create Logger
            Util.CreateLogger(argsOption.LogDirectory, argsOption.LogName, argsOption.LogTarget);

            // Create Rpc server
            var server = new RpcServer().Create(argsOption.HostName, argsOption.RpcPort);

            // Start Rpc server
            await server.Start();
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