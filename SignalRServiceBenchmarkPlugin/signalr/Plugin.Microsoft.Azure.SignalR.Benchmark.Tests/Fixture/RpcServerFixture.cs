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
        protected string _slaveEndpoint = "localhost";
        protected int _port = 5555;
        protected IRpcServer _slaveServer;

        protected readonly ITestOutputHelper _output;
        protected IPlugin _plugin;
        protected IList<IRpcClient> _clients;

        public RpcServerFixture(ITestOutputHelper output)
        {
            // use local signalr as app server
            Environment.SetEnvironmentVariable("useLocalSignalR", "true");
            _output = output;
            if (StartSlave())
            {
                _output.WriteLine("Slave started");
                StartMaster().Wait();
            }
            else
            {
                _output.WriteLine("Fail to start slave");
            }
        }

        public void Dispose()
        {
            _slaveServer.Stop().Wait();
        }

        protected bool StartSlave()
        {
            // Create Rpc server
            _slaveServer = new RpcServer().Create(_slaveEndpoint, _port);

            // Start Rpc server
            var t = Task.Run(() => _slaveServer.Start());
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
            _clients = CreateRpcClients(new string[] { $"{_slaveEndpoint}:{_port}" });
            await WaitRpcConnectSuccess(_clients);
            return true;
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

        private async Task WaitRpcConnectSuccess(IList<IRpcClient> clients)
        {
            _output.WriteLine("Connect Rpc slaves...");
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
                    _output.WriteLine($"Fail to connect slaves because of {ex.Message}, retry {i}th time");
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    continue;
                }
                return;
            }

            var message = $"Cannot connect to all slaves.";
            _output.WriteLine(message);
            throw new Exception(message);
        }
    }
}
