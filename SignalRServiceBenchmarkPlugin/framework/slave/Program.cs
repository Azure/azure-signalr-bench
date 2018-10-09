using System;
using CommandLine;
using Grpc.Core;
using System.Threading;
using System.Threading.Tasks;
using Rpc.Service;
using Common;
using Serilog;

namespace Rpc.Slave
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            // Parse args
            var argsOption = ParseArgs(args);

            // Create Logger
            Util.CreateLogger(argsOption.LogDirectory, argsOption.LogName);

            // Create Rpc server
            var server = CreateRpcServer(argsOption.HostName, argsOption.RpcPort);

            // Start Rpc server
            await StartRpcServer(server);
        }

        private static async Task StartRpcServer(Server server)
        {
            Log.Information("Start server...");
            server.Start();
            await Task.Delay(Timeout.Infinite);
        }

        private static ArgsOption ParseArgs(string[] args)
        {
            Log.Information($"Parse arguments...");
            var argsOption = new ArgsOption();
            var result = Parser.Default.ParseArguments<ArgsOption>(args)
                .WithParsed(options => argsOption = options)
                .WithNotParsed(error => 
                {
                    Log.Error($"Error in parsing arguments: {error}");
                    Environment.Exit(128);
                });
            return argsOption;
        }

        private static Server CreateRpcServer(string hostname, int port)
        {
            Log.Information("Create Rpc Server...");
            Grpc.Core.Server server = new Grpc.Core.Server(new ChannelOption[]
            {
                // For Group, the received message size is very large, so here set 8000k
                new ChannelOption(ChannelOptions.MaxReceiveMessageLength, 8192000)
            })
            {
                Services = { RpcService.BindService(new RpcServiceImpl()) },
                Ports = { new ServerPort(hostname, port, ServerCredentials.Insecure) }
            };

            return server;
        }

    }
}