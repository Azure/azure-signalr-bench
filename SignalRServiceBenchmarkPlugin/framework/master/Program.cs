using CommandLine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rpc.Service;
using Serilog;
using Common;
using Plugin.Base;
using System.Linq;

namespace Rpc.Master
{
    class Program
    {
        private static readonly int _maxRertryConnect = 100;
        private static IPlugin _plugin;
        private static StepHandler _stepHandler;
        private static TimeSpan _retryInterval = TimeSpan.FromSeconds(1);
        private static TimeSpan _statisticsCollectInterval = TimeSpan.FromSeconds(1);

        static async Task Main(string[] args)
        {
            // Parse args
            var argsOption = ParseArgs(args);

            // Create Logger
            Util.CreateLogger(argsOption.LogDirectory, argsOption.LogName, argsOption.LogTarget);

            // Create rpc clients
            var clients = CreateRpcClients(argsOption.SlaveList);

            // Check rpc connections
            await WaitRpcConnectSuccess(clients);

            // Load benchmark configuration
            var configuration = Util.ReadFile(argsOption.BenchmarkConfiguration);
            var benchmarkConfiguration = new BenchmarkConfiguration(configuration);

            // Install plugin in master and slaves
            await InstallPlugin(clients, benchmarkConfiguration.ModuleName);

            // Install serializer and deserializer from plugin
            InstallSerializerAndDeserializer(_plugin, clients);

            // Process pipeline
            await ProcessPipeline(benchmarkConfiguration.Pipeline, clients);
        }

        private static void InstallSerializerAndDeserializer(IPlugin plugin, IList<IRpcClient> clients)
        {
            foreach(var client in clients)
            {
                client.InstallSerializerAndDeserializer(_plugin.Serialize, _plugin.Deserialize);
            }
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

        private static async Task InstallPlugin(IList<IRpcClient> clients, string moduleName)
        {
            Log.Information($"Install plugin '{moduleName}' in master...");
            InstallPluginInMaster(moduleName);
            await InstallPluginInSlaves(clients, moduleName);
        }

        private static void InstallPluginInMaster(string moduleName)
        {
            var type = Type.GetType(moduleName);
            _plugin = (IPlugin)Activator.CreateInstance(type);
            _stepHandler = new StepHandler(_plugin);
        }

        private static async Task InstallPluginInSlaves(IList<IRpcClient> clients, string moduleName)
        {
            Log.Information($"Install plugin in slaves...");

            var tasks = new List<Task<bool>>();

            // Try to install plugin
            var installResults = await Task.WhenAll(from client in clients
                                                    select client.InstallPluginAsync(moduleName));
            var success = installResults.All(result => result);

            if (!success) throw new Exception("Fail to install plugin in slaves.");
        }

        private static async Task ProcessPipeline(
            IList<IList<MasterStep>> pipeline,
            IList<IRpcClient> clients)
        {
            foreach(var parallelStep in pipeline)
            {
                var tasks = new List<Task>();
                foreach(var step in parallelStep)
                {
                    tasks.Add(_stepHandler.HandleStep(step, clients));
                }
                await Task.WhenAll(tasks);
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
