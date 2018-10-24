using CommandLine;
using Common;
using Medallion.Shell;
using Renci.SshNet;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Commander
{
    public class RemoteClients
    {
        public ScpClient AppserverScpClient { get; private set; }
        public ScpClient MasterScpClient { get; private set; }
        public IList<ScpClient> SlaveScpClients { get; private set; }

        public SshClient AppserverSshClient { get; private set; }
        public SshClient MasterSshClient { get; private set; }
        public IList<SshClient> SlaveSshClients { get; private set; }

        public void CreateAll(string username, string password, string appserverHostname, string masterHostname, IList<string> slaveHostnames)
        {
            Log.Information($"Create all remote clients...");

            AppserverScpClient = new ScpClient(appserverHostname, username, password);
            MasterScpClient = new ScpClient(masterHostname, username, password);
            SlaveScpClients = (from hostname in slaveHostnames
                                select new ScpClient(hostname, username, password)).ToList();

            AppserverSshClient = new SshClient(appserverHostname, username, password);
            MasterSshClient = new SshClient(masterHostname, username, password);
            SlaveSshClients = (from hostname in slaveHostnames
                                select new SshClient(hostname, username, password)).ToList();
        }

        private Task Connect(BaseClient client) =>
                Task.Run(() =>
                {
                    try
                    {
                        client.Connect();
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Connect to {client.ConnectionInfo.Host} error: {ex}");
                        throw;
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
            tasks.Add(Connect(AppserverScpClient));
            tasks.Add(Connect(MasterScpClient));
            tasks.Add(Connect(AppserverSshClient));
            tasks.Add(Connect(MasterSshClient));
            Task.WhenAll(tasks).Wait();

            Util.BatchProcess(SlaveScpClients, Connect, 100).Wait();
            Util.BatchProcess(SlaveSshClients, Connect, 100).Wait();
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
                var disconnectTasks = new List<Task>();
                disconnectTasks.Add(Disconnect(AppserverScpClient));
                disconnectTasks.Add(Disconnect(MasterScpClient));
                disconnectTasks.AddRange(from client in SlaveScpClients select Disconnect(client));
                disconnectTasks.Add(Disconnect(AppserverSshClient));
                disconnectTasks.Add(Disconnect(MasterSshClient));
                disconnectTasks.AddRange(from client in SlaveSshClients select Disconnect(client));
                Task.WhenAll(disconnectTasks).Wait();
            }
            finally
            {
                var disposeTasks = new List<Task>();
                disposeTasks.Add(Dispose(AppserverScpClient));
                disposeTasks.Add(Dispose(MasterScpClient));
                disposeTasks.AddRange(from client in SlaveScpClients select Dispose(client));
                disposeTasks.Add(Dispose(AppserverSshClient));
                disposeTasks.Add(Dispose(MasterSshClient));
                disposeTasks.AddRange(from client in SlaveSshClients select Dispose(client));
                Task.WhenAll(disposeTasks).Wait();
            }
        }
    }
}
