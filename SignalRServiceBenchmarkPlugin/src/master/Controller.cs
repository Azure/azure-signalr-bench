using Common;
using Grpc.Core;
using Grpc.Core.Logging;
using Plugin.Base;
using Rpc.Service;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rpc.Master
{
    public class Controller
    {
        private static readonly int _maxRertryConnect = 100;
        private static TimeSpan _retryInterval = TimeSpan.FromSeconds(1);
        private RpcConfig _rpcConfig;

        public Controller(RpcConfig config)
        {
            _rpcConfig = config;
        }

        public async Task Start()
        {
            Util.SavePidToFile(_rpcConfig.PidFile);

            // Create Logger
            RpcUtils.CreateLogger(_rpcConfig.LogDirectory, _rpcConfig.LogName, _rpcConfig.LogTarget);

            var type = Type.GetType(_rpcConfig.PluginFullName);

            var plugin = (IPlugin)Activator.CreateInstance(type);

            if (!CheckUsage(_rpcConfig, plugin))
            {
                // Load benchmark configuration
                var configuration = Util.ReadFile(_rpcConfig.PluginConfiguration);
                IList<IRpcClient> clients = null;
                if (plugin.NeedAgents(configuration))
                {
                    // Create rpc clients
                    clients = CreateRpcClients(_rpcConfig.AgentList);

                    // Check rpc connections
                    await WaitRpcConnectSuccess(clients);
                }

                await plugin.Start(configuration, clients);
            }
        }

        private static bool CheckUsage(RpcConfig argsOption, IPlugin plugin)
        {
            if (argsOption.PluginConfiguration == "?")
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

        private static IList<IRpcClient> CreateRpcClients(IList<string> agentList)
        {
            var hostnamePortList = (from agent in agentList
                                    select agent.Split(':') into parts
                                    select (Hostname: parts[0], Port: Convert.ToInt32(parts[1])));

            var clients = from item in hostnamePortList
                          select RpcClient.Create(item.Hostname, item.Port);

            return clients.ToList();
        }

        private static async Task WaitRpcConnectSuccess(IList<IRpcClient> clients)
        {
            Log.Information("Connect Rpc agents...");
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
                    Log.Warning($"Fail to connect agents because of {ex.Message}, retry {i}th time");
                    await Task.Delay(_retryInterval);
                    continue;
                }
                return;
            }

            var message = $"Cannot connect to all agents.";
            Log.Error(message);
            throw new Exception(message);
        }
    }
}
