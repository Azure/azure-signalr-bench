using CommandLine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rpc.Service;
using Serilog;
using Common;
using Plugin.Base;
using System.Linq;
using Grpc.Core;
using Grpc.Core.Logging;

namespace Rpc.Master
{
    class Program
    {
        private static readonly int _maxRertryConnect = 100;
        private static TimeSpan _retryInterval = TimeSpan.FromSeconds(1);
        private static TimeSpan _statisticsCollectInterval = TimeSpan.FromSeconds(1);

        static async Task Main(string[] args)
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

            var type = Type.GetType(argsOption.PluginFullName);

            var plugin = (IPlugin)Activator.CreateInstance(type);

            if (!CheckUsage(argsOption, plugin))
            {
                // Load benchmark configuration
                var configuration = Util.ReadFile(argsOption.BenchmarkConfiguration);

                plugin.DumpConfiguration(configuration);

                // Create rpc clients
                var clients = CreateRpcClients(argsOption.SlaveList);

                // Check rpc connections
                await WaitRpcConnectSuccess(clients);

                await plugin.Start(configuration, clients);
            }
        }

        private static bool CheckUsage(ArgsOption argsOption, IPlugin plugin)
        {
            if (argsOption.BenchmarkConfiguration == "?")
            {
                plugin.Help();
                return true;
            }
            return false;
        }

        private static void EnableTracing()
        {
            Environment.SetEnvironmentVariable("GRPC_TRACE", "tcp,channel,http,secure_endpoint");
            Environment.SetEnvironmentVariable("GRPC_VERBOSITY", "DEBUG");
            GrpcEnvironment.SetLogger(new ConsoleLogger());
        }

        private static IList<IRpcClient> CreateRpcClients(IList<string> slaveList)
        {
            var hostnamePortList = (from slave in slaveList
                                    select slave.Split(':') into parts
                                    select (Hostname: parts[0], Port: Convert.ToInt32(parts[1])));

            var clients = from item in hostnamePortList
                          select RpcClient.Create(item.Hostname, item.Port);

            return clients.ToList();
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
                    //Log.Error($"Error in parsing arguments: {error}");
                    //throw new ArgumentException("Error in parsing arguments.");
                });
            return argsOption;
        }

        private static async Task WaitRpcConnectSuccess(IList<IRpcClient> clients)
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
                            client.TestConnection();
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"Fail to connect slaves because of {ex.Message}, retry {i}th time");
                    await Task.Delay(_retryInterval);
                    continue;
                }
                return;
            }

            var message = $"Cannot connect to all slaves.";
            Log.Error(message);
            throw new Exception(message);
        }
    }
}
