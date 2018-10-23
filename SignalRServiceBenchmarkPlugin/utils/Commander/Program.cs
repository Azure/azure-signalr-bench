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

// TODO: dispose commands
namespace Commander
{
    class Program
    {
        private static string _baseName = "publish";

        public static void Main(string[] args)
        {
            var argsOption = ParseArgs(args);

            Util.CreateLogger(argsOption.LogDirectory, argsOption.LogName, argsOption.LogTarget);

            // Publish dlls
            var appserverExecutable = PubishExcutable(argsOption.AppserverProject, _baseName);
            var masterExecutable = PubishExcutable(argsOption.MasterProject, _baseName);
            var slaveExecutable = PubishExcutable(argsOption.SlaveProject, _baseName);

            //// DEBUG
            //var baseName = "publish";
            //var appserverExecutable = new FileInfo(Path.Combine(argsOption.AppserverProject, $"{baseName}.zip"));
            //var masterExecutable = new FileInfo(Path.Combine(argsOption.MasterProject, $"{baseName}.zip"));
            //var slaveExecutable = new FileInfo(Path.Combine(argsOption.SlaveProject, $"{baseName}.zip"));

            // Copy executables and configurations
            CopyExcutables(argsOption.Username, argsOption.Password,
                argsOption.AppServerHostname, argsOption.MasterHostname, argsOption.SlaveList,
                appserverExecutable, masterExecutable, slaveExecutable, 
                argsOption.AppserverTargetPath, argsOption.MasterTargetPath, argsOption.SlaveTargetPath,
                new FileInfo(argsOption.BenchmarkConfiguration), argsOption.BenchmarkConfigurationTargetPath);

            SshClient appServerSshClient = null;
            SshClient masterSshClient = null;
            IList<SshClient> slaveSshClients = null;

            try
            {
                // Create Ssh clients
                (appServerSshClient, masterSshClient, slaveSshClients)
                    = CreateSshClients(argsOption.Username, argsOption.Password,
                    argsOption.AppServerHostname, argsOption.MasterHostname, argsOption.SlaveList);

                // Connect to Vms
                ConnectSsh(appServerSshClient, masterSshClient, slaveSshClients);

                // Unzip packages
                Unzip(appServerSshClient, masterSshClient, slaveSshClients,
                        argsOption.AppserverTargetPath, argsOption.MasterTargetPath, argsOption.SlaveTargetPath);

                // Run benchmark
                RunBenchmark(appServerSshClient, masterSshClient, slaveSshClients,
                             argsOption.AppserverTargetPath, argsOption.MasterTargetPath, argsOption.SlaveTargetPath,
                             _baseName, argsOption.BenchmarkConfigurationTargetPath, string.Join(',', argsOption.SlaveList), 
                             argsOption.RpcPort, argsOption.AppserverPort);
            }
            finally
            {
                // Disconnect
                appServerSshClient.Disconnect();
                masterSshClient.Disconnect();
                slaveSshClients.ToList().ForEach(client => client.Disconnect());

                // Dispose
                appServerSshClient.Dispose();
                masterSshClient.Dispose();
                slaveSshClients.ToList().ForEach(client => client.Dispose());
            }
        }

        private static void Unzip(SshClient appServerSshClient, SshClient masterSshClient, IList<SshClient> slaveSshClients, 
                                    string appserverTargetPath, string masterTargetPath, string slaveTargetPath)
        {
            Log.Information($"Unzip executables");

            var tasks = new List<Task>();
            tasks.Add(Task.Run(() => Unzip(appServerSshClient, appserverTargetPath)));
            tasks.Add(Task.Run(() => Unzip(masterSshClient, masterTargetPath)));
            tasks.AddRange(from client in slaveSshClients
                           select Task.Run(() => Unzip(client, slaveTargetPath)));
            Task.WhenAll(tasks).Wait();
        }

        

        private static void InstallZip(SshClient client)
        {
            var command = client.CreateCommand("sudo apt-get install -y zip");
            var result = command.Execute();
            if (command.Error != "")
            {
                Log.Warning(result);
            }
        }

        private static void Unzip(SshClient client, string targetPath)
        {
            InstallZip(client);

            var directory = Path.GetDirectoryName(targetPath);
            var filenameWithoutExtension = Path.GetFileNameWithoutExtension(targetPath);
            var command = client.CreateCommand($"unzip -o -d {filenameWithoutExtension} {targetPath}");
            var result = command.Execute();
            if (command.Error != "")
            {
                throw new Exception(command.Error);
            }
        }

        private static void ConnectSsh(SshClient appServerSshClient, SshClient masterSshClient, IList<SshClient> slaveSshClients)
        {
            var tasks = new List<Task>();
            tasks.Add(Task.Run(() => appServerSshClient.Connect()));
            tasks.Add(Task.Run(() => masterSshClient.Connect()));
            tasks.AddRange(from client in slaveSshClients
                           select Task.Run(() => client.Connect()));
            Task.WhenAll(tasks).Wait();
        }

        private static void RunBenchmark(SshClient appServerSshClient, SshClient masterSshClient, IList<SshClient> slaveSshClients,
            string appserverTargetPath, string masterTargetPath, string slaveTargetPath, string baseName, string benchmarkConfiguration, string slaveList,
            int rpcPort, int appserverPort)
        {
            try
            {
                Log.Information($"Run benchmark");

                // Kill all dotnet
                Log.Information($"Kill all dotnet processes");
                KillallDotnet(appServerSshClient, masterSshClient);
                KillallDotnet(slaveSshClients.ToArray());

                // Create SSH commands
                var appserverDirectory = Path.Combine(Path.GetDirectoryName(appserverTargetPath), Path.GetFileNameWithoutExtension(appserverTargetPath), baseName);
                var appseverCommand = $"export useLocalSignalR=true; cd {appserverDirectory}; dotnet exec AppServer.dll --urls=http://*:{appserverPort}";
                var masterCommand = $"dotnet exec {Path.Combine(Path.GetDirectoryName(masterTargetPath), Path.GetFileNameWithoutExtension(masterTargetPath), baseName, "master.dll")} -- --BenchmarkConfiguration=\"{benchmarkConfiguration}\" --SlaveList=\"{slaveList}\"";
                var slaveCommand = $"dotnet exec {Path.Combine(Path.GetDirectoryName(slaveTargetPath), Path.GetFileNameWithoutExtension(slaveTargetPath), baseName, "slave.dll")} --HostName 0.0.0.0 --RpcPort {rpcPort}";
                var appserverSshCommand = appServerSshClient.CreateCommand(appseverCommand.Replace('\\', '/'));
                var masterSshCommand = masterSshClient.CreateCommand(masterCommand.Replace('\\', '/'));
                var slaveSshCommands = (from client in slaveSshClients select client.CreateCommand(slaveCommand.Replace('\\', '/'))).ToList();

                // Start app server
                Log.Information($"Start app server");
                var appserverAsyncResult = appserverSshCommand.BeginExecute();

                // Start slaves
                Log.Information($"Start slaves");
                var slaveAsyncResults = (from command in slaveSshCommands select command.BeginExecute()).ToList();
                
                // Start master
                Log.Information($"Start master");
                var masterResult = masterSshCommand.Execute();
            }
            finally
            {
                // Kill all dotnet
                KillallDotnet(appServerSshClient, masterSshClient);
                KillallDotnet(slaveSshClients.ToArray());
            }

        }

        private static void KillallDotnet(params SshClient[] clients)
        {
            if (clients == null)
            {
                throw new ArgumentNullException(nameof(clients));
            }

            foreach(var client in clients)
            {
                var command = client.CreateCommand("killall dotnet");
                var result = command.Execute();
            }
        }

        private static (SshClient, SshClient, IList<SshClient>) CreateSshClients(string username, string password,
            string appserverHostname, string masterHostname, IList<string> slaveHostnames)
        {
            return (new SshClient(appserverHostname, username, password),
                new SshClient(masterHostname, username, password), 
                (from slave in slaveHostnames
                 let hostname = slave.Split(':')[0]
                 select new SshClient(hostname, username, password)).ToList());
        }

        private static void CopyExcutables(string username, string password, 
            string appserverHostname, string masterHostname, IList<string> slaveHostnames,
            FileInfo appserverExecutable, FileInfo masterExecutable, FileInfo slaveExecutable,
            string appserverTargetPath, string masterTargetPath, string slaveTargetPath,
            FileInfo benchmarkConfiguration, string benchmarkConfigurationTargetPath)
        {
            Log.Information($"Copy executables to hosts...");

            ScpClient appServerScpClient = null;
            ScpClient masterScpClient = null;
            List<ScpClient> slaveScpClients = null;

            try
            {
                // Create scp client
                appServerScpClient = new ScpClient(appserverHostname, username, password);
                masterScpClient = new ScpClient(masterHostname, username, password);
                slaveScpClients = (from slave in slaveHostnames
                                   let hostname = slave.Split(':')[0]
                                   select new ScpClient(hostname, username, password)).ToList();

                // Connect to host
                var connectTasks = new List<Task>();
                connectTasks.Add(Task.Run(() => appServerScpClient.Connect()));
                connectTasks.Add(Task.Run(() => masterScpClient.Connect()));
                connectTasks.AddRange(from slaveScpClient in slaveScpClients
                                      select Task.Run(() => slaveScpClient.Connect()));
                Task.WhenAll(connectTasks).Wait();

                // Copy executables
                var copyTasks = new List<Task>();
                copyTasks.Add(Task.Run(() => appServerScpClient.Upload(appserverExecutable, appserverTargetPath)));
                copyTasks.Add(Task.Run(() => masterScpClient.Upload(masterExecutable, masterTargetPath)));
                copyTasks.Add(Task.Run(() => masterScpClient.Upload(benchmarkConfiguration, benchmarkConfigurationTargetPath)));
                copyTasks.AddRange(from slaveScpClient in slaveScpClients
                                   select Task.Run(() => slaveScpClient.Upload(slaveExecutable, slaveTargetPath)));
                Task.WhenAll(copyTasks).Wait();
            }
            finally
            {
                // Disconnect
                appServerScpClient.Disconnect();
                masterScpClient.Disconnect();
                slaveScpClients.ForEach(client => client.Disconnect());

                // Dispose
                appServerScpClient.Dispose();
                masterScpClient.Dispose();
                slaveScpClients.ForEach(client => client.Dispose());
            }

        }

        private static FileInfo PubishExcutable(string projectPath, string baseName)
        {
            var publish = Command.Run("dotnet", $"publish -o {baseName}".Split(' '), o => o.WorkingDirectory(projectPath));
            publish.Wait();
            if (!publish.Result.Success) throw new Exception(publish.Result.StandardOutput);

            var zip = Command.Run("zip", new string[] { "-r", $"{baseName}.zip", $"{baseName}/" }, o => o.WorkingDirectory(projectPath));
            zip.Wait();
            if (!zip.Result.Success) throw new Exception(zip.Result.StandardOutput);

            return new FileInfo(Path.Combine(projectPath, $"{baseName}.zip"));
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
                    throw new ArgumentException($"Error in parsing arguments: {error}");
                });
            return argsOption;
        }
    }
}
