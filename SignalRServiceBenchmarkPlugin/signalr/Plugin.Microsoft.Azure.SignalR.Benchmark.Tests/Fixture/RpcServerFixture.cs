using Newtonsoft.Json;
using Plugin.Base;
using Plugin.Microsoft.Azure.SignalR.Benchmark.Internals;
using Rpc.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.Tests
{
    public abstract class RpcServerFixture : IDisposable
    {
        protected string _agentEndpoint = "localhost";
        protected int _port = 5555;
        protected IRpcServer _agentServer;

        protected readonly ITestOutputHelper _output;
        protected IPlugin _plugin;
        protected IList<IRpcClient> _clients;

        public RpcServerFixture(ITestOutputHelper output)
        {
            // use local signalr as app server
            Environment.SetEnvironmentVariable("useLocalSignalR", "true");
            _output = output;
            if (StartAgent())
            {
                _output.WriteLine("Agent started");
                StartMaster().Wait();
            }
            else
            {
                _output.WriteLine("Fail to start agent");
            }
        }

        public void Dispose()
        {
            _agentServer.Stop().Wait();
        }

        protected bool StartAgent()
        {
            // Create Rpc server
            _agentServer = new RpcServer().Create(_agentEndpoint, _port);

            // Start Rpc server
            var t = Task.Run(() => _agentServer.Start());
            Task.Delay(TimeSpan.FromSeconds(5));
            if (t.IsFaulted)
            {
                return false;
            }
            return true;
        }

        protected async Task<bool> StartMaster()
        {
            var type = Type.GetType("Plugin.Microsoft.Azure.SignalR.Benchmark.SignalRBenchmarkPlugin, Plugin.Microsoft.Azure.SignalR.Benchmark");

            _plugin = (IPlugin)Activator.CreateInstance(type);
            _clients = CreateRpcClients(new string[] { $"{_agentEndpoint}:{_port}" });
            await WaitRpcConnectSuccess(_clients);
            return true;
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

        private async Task WaitRpcConnectSuccess(IList<IRpcClient> clients)
        {
            _output.WriteLine("Connect Rpc agents...");
            for (var i = 0; i < 5; i++)
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
                    _output.WriteLine($"Fail to connect agents because of {ex.Message}, retry {i}th time");
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    continue;
                }
                return;
            }

            var message = $"Cannot connect to all agents.";
            _output.WriteLine(message);
            throw new Exception(message);
        }
    }
}
