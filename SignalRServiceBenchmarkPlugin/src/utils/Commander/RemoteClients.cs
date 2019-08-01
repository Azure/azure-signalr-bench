using Common;
using Renci.SshNet;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Commander
{
    public class RemoteClients
    {
        private static readonly TimeSpan KeepAliveSpan = TimeSpan.FromSeconds(15);
        public IList<ScpClient> AppserverScpClients { get; private set; }
        public ScpClient MasterScpClient { get; private set; }
        public IList<ScpClient> AgentScpClients { get; private set; }

        public IList<SshClient> AppserverSshClients { get; private set; }
        public SshClient MasterSshClient { get; private set; }
        public IList<SshClient> AgentSshClients { get; private set; }

        public void CreateAll(string username, string password,
            IList<string> appserverHostnames, string masterHostname,
            IList<string> agentHostnames)
        {
            Log.Information($"Create all remote clients...");

            if (appserverHostnames != null)
            {
                AppserverScpClients = (from hostname in appserverHostnames
                                       select new ScpClient(hostname, username, password)).ToList();
                AppserverSshClients = (from hostname in appserverHostnames
                                       select new SshClient(hostname, username, password)).ToList();
            }
            MasterScpClient = new ScpClient(masterHostname, username, password);
            AgentScpClients = (from hostname in agentHostnames
                                select new ScpClient(hostname, username, password)).ToList();
            MasterSshClient = new SshClient(masterHostname, username, password);
            AgentSshClients = (from hostname in agentHostnames
                                select new SshClient(hostname, username, password)).ToList();
        }

        private Task Connect(BaseClient client) =>
                Task.Run(async () =>
                {
                    var i = 0;
                    var maxTry = 5;
                    while (i < maxTry)
                    {
                        try
                        {
                            client.KeepAliveInterval = KeepAliveSpan; // avoid client drops
                            client.Connect();
                            break;
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Connect to {client.ConnectionInfo.Host} error: {ex}");
                            if (i == maxTry)
                                throw;
                            else
                            {
                                Log.Information($"retry connect to the client");
                                await Task.Delay(TimeSpan.FromSeconds(5));
                            }
                        }
                        i++;
                    }
                });

        private Task Disconnect(BaseClient client) =>
                Task.Run(() =>
                {
                    try
                    {
                        client.Disconnect();
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Disconnect to {client.ConnectionInfo.Host} error: {ex}");
                        throw;
                    }
                });

        public void ConnectAll()
        {
            Log.Information($"Connect all remote clients...");

            var tasks = new List<Task>();
            tasks.Add(Connect(MasterScpClient));
            tasks.Add(Connect(MasterSshClient));
            Task.WhenAll(tasks).Wait();

            if (AppserverScpClients != null)
            {
                Util.BatchProcess(AppserverScpClients, Connect, 10).Wait();
            }
            if (AppserverSshClients != null)
            {
                Util.BatchProcess(AppserverSshClients, Connect, 10).Wait();
            }
            Util.BatchProcess(AgentScpClients, Connect, 10).Wait();
            Util.BatchProcess(AgentSshClients, Connect, 10).Wait();
        }

        private Task Dispose(BaseClient client) =>
                Task.Run(() =>
                {
                    try
                    {
                        client.Disconnect();
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Dispose to {client.ConnectionInfo.Host} error: {ex}");
                        throw;
                    }
                });

        public void DestroyAll()
        {
            Log.Information($"Destroy all remote clients...");

            try
            {
                Log.Information($"Disconnect all remote clients...");

                var disconnectTasks = new List<Task>();
                if (AppserverScpClients != null)
                {
                    disconnectTasks.AddRange(from client in AppserverScpClients select Disconnect(client));
                }
                if (AppserverSshClients != null)
                {
                    disconnectTasks.AddRange(from client in AppserverSshClients select Disconnect(client));
                }
                disconnectTasks.Add(Disconnect(MasterScpClient));
                disconnectTasks.AddRange(from client in AgentScpClients select Disconnect(client));
                disconnectTasks.Add(Disconnect(MasterSshClient));
                disconnectTasks.AddRange(from client in AgentSshClients select Disconnect(client));
                Task.WhenAll(disconnectTasks).Wait(TimeSpan.FromSeconds(30));
            }
            finally
            {
                Log.Information($"Dispose all remote clients...");
                var disposeTasks = new List<Task>();
                if (AppserverScpClients != null)
                {
                    disposeTasks.AddRange(from client in AppserverScpClients select Dispose(client));
                }
                disposeTasks.Add(Dispose(MasterScpClient));
                disposeTasks.AddRange(from client in AgentScpClients select Dispose(client));
                if (AppserverSshClients != null)
                {
                    disposeTasks.AddRange(from client in AppserverSshClients select Dispose(client));
                }
                disposeTasks.Add(Dispose(MasterSshClient));
                disposeTasks.AddRange(from client in AgentSshClients select Dispose(client));
                Task.WhenAll(disposeTasks).Wait(TimeSpan.FromSeconds(60));
                Log.Information("Finish destroying all");
            }
        }
    }
}
