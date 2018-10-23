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
            AppserverScpClient = new ScpClient(appserverHostname, username, password);
            MasterScpClient = new ScpClient(masterHostname, username, password);
            SlaveScpClients = (from hostname in slaveHostnames
                                select new ScpClient(hostname, username, password)).ToList();

            AppserverSshClient = new SshClient(appserverHostname, username, password);
            MasterSshClient = new SshClient(masterHostname, username, password);
            SlaveSshClients = (from hostname in slaveHostnames
                                select new SshClient(hostname, username, password)).ToList();
        }

        public void ConnectAll()
        {
            var tasks = new List<Task>();
            tasks.Add(Task.Run(() => AppserverScpClient.Connect()));
            tasks.Add(Task.Run(() => MasterScpClient.Connect()));
            tasks.AddRange(from client in SlaveScpClients select Task.Run( () => client.Connect()));
            tasks.Add(Task.Run(() => AppserverSshClient.Connect()));
            tasks.Add(Task.Run(() => MasterSshClient.Connect()));
            tasks.AddRange(from client in SlaveSshClients select Task.Run( () => client.Connect()));
            Task.WhenAll(tasks).Wait();
        }

        public void DestroyAll()
        {
            var disconnectTasks = new List<Task>();
            disconnectTasks.Add(Task.Run(() => AppserverScpClient.Disconnect()));
            disconnectTasks.Add(Task.Run(() => MasterScpClient.Disconnect()));
            disconnectTasks.AddRange(from client in SlaveScpClients select Task.Run(() => client.Disconnect()));
            disconnectTasks.Add(Task.Run(() => AppserverSshClient.Disconnect()));
            disconnectTasks.Add(Task.Run(() => MasterSshClient.Disconnect()));
            disconnectTasks.AddRange(from client in SlaveSshClients select Task.Run(() => client.Disconnect()));
            Task.WhenAll(disconnectTasks).Wait();

            var disposeTasks = new List<Task>();
            disposeTasks.Add(Task.Run(() => AppserverScpClient.Dispose()));
            disposeTasks.Add(Task.Run(() => MasterScpClient.Dispose()));
            disposeTasks.AddRange(from client in SlaveScpClients select Task.Run(() => client.Dispose()));
            disposeTasks.Add(Task.Run(() => AppserverSshClient.Dispose()));
            disposeTasks.Add(Task.Run(() => MasterSshClient.Dispose()));
            disposeTasks.AddRange(from client in SlaveSshClients select Task.Run(() => client.Dispose()));
            Task.WhenAll(disposeTasks).Wait();
        }
    }
}
