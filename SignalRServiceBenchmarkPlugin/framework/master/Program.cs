using CommandLine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rpc.Service;
using Serilog;
using Common;
using Plugin.Base;

namespace Rpc.Master
{
    class Program
    {
        private static readonly int _maxRertryConnect = 100;
        private static IPlugin _plugin;

        static async Task Main(string[] args)
        {
            // Parse args
            var argsOption = ParseArgs(args);

            // Create Logger
            Util.CreateLogger(argsOption.LogDirectory, argsOption.LogName, argsOption.LogTarget);

            // Create rpc clients
            var clients = CreateRpcClients(argsOption.SlaveList, argsOption.RpcPort);

            // Check rpc connections
            WaitRpcConnectSuccess(clients);

            // Load benchmark configuration
            var configuration = Util.ReadFile(argsOption.BenchmarkConfiguration);
            var benchmarkConfiguration = new BenchmarkConfiguration(configuration);

            // Install plugin in master and slaves
            await InstallPlugin(clients, benchmarkConfiguration.ModuleName);

            // Process pipeline
            await ProcessPipeline(benchmarkConfiguration.Pipeline, clients);
        }
        
        private static IList<IRpcClient> CreateRpcClients(IList<string> slaveList, int rpcPort)
        {
            var clients = new List<IRpcClient>();
            foreach(var slave in slaveList)
            {
                var client = new RpcClient().Create(slave, rpcPort);
                clients.Add(client);
            }
            return clients;
        }

        private static async Task InstallPlugin(IList<IRpcClient> clients, string moduleName)
        {
            Log.Information($"Install plugin...");
            InstallPluginInMaster(moduleName);
            await InstallPluginInSlaves(clients, moduleName);
        }

        private static void InstallPluginInMaster(string moduleName)
        {
            var type = Type.GetType(moduleName);
            _plugin = (IPlugin)Activator.CreateInstance(type);
        }

        private static async Task InstallPluginInSlaves(IList<IRpcClient> clients, string moduleName)
        {
            Log.Information($"Install plugin in slaves...");

            var tasks = new List<Task<bool>>();

            // Try to install plugin
            foreach (var client in clients)
            {
                tasks.Add(client.InstallPluginAsync(moduleName));
            }
            await Task.WhenAll(tasks);

            // Check whether the plugin installed
            var success = true;
            foreach (var task in tasks)
            {
                if (!task.Result) success = false;
                break;
            }

            if (!success) throw new Exception("Fail to install plugin in slaves.");
        }

        private static async Task ProcessPipeline(IList<IList<MasterStep>> pipeline, IList<IRpcClient> clients)
        {
            foreach(var parallelStep in pipeline)
            {
                foreach(var step in parallelStep)
                {
                    var tasks = new List<Task>();
                    tasks.Add(_plugin.HandleMasterStep(step, clients));
                    await Task.WhenAll(tasks);
                }
            }
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
                    throw new ArgumentException("Error in parsing arguments.");
                });
            return argsOption;
        }

        private static void WaitRpcConnectSuccess(IList<IRpcClient> clients)
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
                    Log.Warning($"Fail to connect slaves, retry {i}th time");
                    continue;
                }
                break;
            }
        }
    }
}
