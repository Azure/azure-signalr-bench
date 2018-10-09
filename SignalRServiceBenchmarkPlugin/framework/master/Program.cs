using CommandLine;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rpc.Service;
using Serilog;
using System.IO;
using Common;

namespace Rpc.Master
{
    class Program
    {
        private static readonly int _maxRertryConnect = 100;

        static async Task Main(string[] args)
        {
            // Parse args
            var argsOption = ParseArgs(args);

            // Create Logger
            Util.CreateLogger(argsOption.LogDirectory, argsOption.LogName);

            // Generate rpc channels
            var channels = CreateRpcChannels(argsOption.SlaveList, argsOption.RpcPort);

            // Create rpc clients
            var clients = CreateRpcClients(channels);

            // Check rpc connections
            WaitRpcConnectSuccess(clients);
        }

        private static ArgsOption ParseArgs(string[] args)
        {
            Log.Information($"Parse arguments...");
            var argsOption = new ArgsOption();
            var result = Parser.Default.ParseArguments<ArgsOption>(args)
                .WithParsed(options => argsOption = options)
                .WithNotParsed(error => { });
            return argsOption;
        }

        private static List<Channel> CreateRpcChannels(IList<string> slaveList, int rpcPort)
        {
            Log.Information("Open channel to rpc servers...");
            var channels = new List<Channel>(slaveList.Count);
            for (var i = 0; i < slaveList.Count; i++)
            {
                channels.Add(new Channel($"{slaveList[i]}:{rpcPort}", ChannelCredentials.Insecure,
                    new ChannelOption[] {
                        // For Group, the received message size is very large, so here set 8000k
                        new ChannelOption(ChannelOptions.MaxReceiveMessageLength, 8192000)
                    }));
            }

            return channels;
        }

        private static List<RpcService.RpcServiceClient> CreateRpcClients(List<Channel> channels)
        {
            Log.Information($"Create Rpc clients...");
            var clients = new List<RpcService.RpcServiceClient>();
            for (var i = 0; i < channels.Count; i++)
            {
                clients.Add(new RpcService.RpcServiceClient(channels[i]));
            }
            return clients;
        }

        private static void WaitRpcConnectSuccess(List<RpcService.RpcServiceClient> clients)
        {
            Log.Information("Connect Rpc slaves...");
            for (var i = 0; i < _maxRertryConnect; i++)
            {
                try
                {
                    foreach (var client in clients)
                    {
                        try
                        {
                            client.TestConnection(new Empty());
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"Fail to connect slaves, retry {i}th time");
                    continue;
                }
                break;
            }
        }
    }
}
