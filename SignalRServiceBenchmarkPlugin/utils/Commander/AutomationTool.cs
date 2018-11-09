using Medallion.Shell;
using Renci.SshNet;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commander
{
    public class AutomationTool
    {
        // Executable information
        private readonly string _baseName = "publish";

        // App server information
        private readonly int _appserverPort;
        private readonly string _azureSignalRConnectionString;
        private readonly string _appserverLogDirPath;

        // Slave Information
        private readonly int _rpcPort;
        private readonly IList<string> _slaveList;

        // Local project path
        private readonly string _appserverProject;
        private readonly string _masterProject;
        private readonly string _slaveProject;

        // Remote executable files
        private readonly string _appserverTargetPath;
        private readonly string _masterTargetPath;
        private readonly string _slaveTargetPath;

        // Benchmark configuration
        private readonly FileInfo _benchmarkConfiguration;

        // Remote benchmark configuration
        private readonly string _benchmarkConfigurationTargetPath;

        // Scp/Ssh clients
        private RemoteClients _remoteClients;
        private AsyncCallback _sshCmdCallback;

        public AutomationTool(ArgsOption argOption)
        {
            // Initialize
            _appserverProject = argOption.AppserverProject;
            _appserverLogDirPath = argOption.AppserverLogDirectory;
            _masterProject = argOption.MasterProject;
            _slaveProject = argOption.SlaveProject;
            _appserverTargetPath = argOption.AppserverTargetPath;
            _masterTargetPath = argOption.MasterTargetPath;
            _slaveTargetPath = argOption.SlaveTargetPath;
            _benchmarkConfiguration = new FileInfo(argOption.BenchmarkConfiguration);
            _benchmarkConfigurationTargetPath = argOption.BenchmarkConfigurationTargetPath;
            _slaveList = argOption.SlaveList;
            _rpcPort = argOption.RpcPort;
            _appserverPort = argOption.AppserverPort;
            _azureSignalRConnectionString = argOption.AzureSignalRConnectionString;
            _sshCmdCallback = new AsyncCallback(OnAsyncSshCommandComplete);
            // Create clients
            var slaveHostnames = (from slave in argOption.SlaveList select slave.Split(':')[0]).ToList();
            _remoteClients = new RemoteClients();
            _remoteClients.CreateAll(argOption.Username, argOption.Password, 
                argOption.AppServerHostnames, argOption.MasterHostname, slaveHostnames);
        }

        public void Start()
        {
            try
            {
                // Clients connect to host
                _remoteClients.ConnectAll();

                // Publish dlls
                var (appserverExecutable, masterExecutable, slaveExecutable) = PubishExcutables();

                // Copy executables and configurations
                CopyExcutables(appserverExecutable, masterExecutable, slaveExecutable);

                // Unzip packages
                UnzipExecutables();

                // Run benchmark
                RunBenchmark();
            }
            finally
            {
                // Disconnect and dispose
                _remoteClients.DestroyAll();
            }
        }

        // Only for development
        public void StartDev()
        {
            try
            {
                // Clients connect to host
                _remoteClients.ConnectAll();

                FileInfo appserverExecutable, masterExecutable, slaveExecutable;
                appserverExecutable = File.Exists($"{_appserverProject}/{_baseName}.tgz") ?
                    new FileInfo($"{_appserverProject}/{_baseName}.tgz") :
                    new FileInfo($"{_appserverProject}/{_baseName}.zip");
                masterExecutable = File.Exists($"{_masterProject}/{_baseName}.tgz") ?
                    new FileInfo($"{_masterProject}/{_baseName}.tgz") :
                    new FileInfo($"{_masterProject}/{_baseName}.zip");
                slaveExecutable = File.Exists($"{_slaveProject}/{_baseName}.tgz") ?
                    new FileInfo($"{_slaveProject}/{_baseName}.tgz") :
                    new FileInfo($"{_slaveProject}/{_baseName}.zip");

                // Copy executables and configurations
                CopyExcutables(appserverExecutable, masterExecutable, slaveExecutable);

                // Unzip packages
                UnzipExecutables();

                // Run benchmark
                RunBenchmark();
            }
            finally
            {
                // Disconnect and dispose
                _remoteClients.DestroyAll();
            }
        }

        private void OnAsyncSshCommandComplete(IAsyncResult result)
        {
            var command = (SshCommand)result.AsyncState;
            if (command.ExitStatus != 0)
            {
                Log.Error($"SshCommand '{command.CommandText}' occurs error: {command.Error}");
                throw new Exception(command.Error);
            }
        }

        private void WaitAppserverStarted(List<SshCommand> appservers)
        {
            var timeout = 600;
            for (var i = 0; i < appservers.Count; i++)
            {
                var recheck = 0;
                while (recheck < timeout)
                {
                    using (var reader =
                        new StreamReader(appservers[i].OutputStream, Encoding.UTF8, true, 4096))
                    {
                        var applog = reader.ReadToEnd();
                        if (applog.Contains("HttpConnection Started"))
                        {
                            Log.Information($"appserver '{appservers[i]}' started");
                            break;
                        }
                        else
                        {
                            Log.Information($"waiting for appserver '{appservers[i]}' starting...");
                            recheck++;
                            Task.Delay(TimeSpan.FromSeconds(1)).Wait();
                        }
                    }
                }
            }
        }

        private void CopyAppServerLog(List<SshCommand> appservers)
        {
            for (var i = 0; i < appservers.Count; i++)
            {
                var cmd = appservers[i];
                var logFile = $"applog{i}.log";

                if (!String.IsNullOrEmpty(_appserverLogDirPath) && !Directory.Exists(_appserverLogDirPath))
                {
                    Directory.CreateDirectory(_appserverLogDirPath);
                }
                var logFilePath = System.IO.Path.GetDirectoryName(Path.Combine(_appserverLogDirPath, logFile));

                using (var reader = new StreamReader(cmd.OutputStream, Encoding.UTF8, true, 1024))
                using (var writer = new StreamWriter(logFilePath, true))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        if (line != null)
                        {
                            writer.Write(line);
                        }
                    }
                }
            }
        }

        private void RunBenchmark()
        {
            try
            {
                Log.Information($"Run benchmark");

                // Kill all dotnet
                Log.Information($"Kill all dotnet processes");
                KillallDotnet();

                // Create SSH commands
                var appserverDirectory = Path.Combine(Path.GetDirectoryName(_appserverTargetPath),
                    Path.GetFileNameWithoutExtension(_appserverTargetPath), _baseName);
                var masterExecutablePath = Path.Combine(Path.GetDirectoryName(_masterTargetPath),
                    Path.GetFileNameWithoutExtension(_masterTargetPath), _baseName, "master.dll");
                var slaveExecutablePath = Path.Combine(Path.GetDirectoryName(_slaveTargetPath),
                    Path.GetFileNameWithoutExtension(_slaveTargetPath), _baseName, "slave.dll");
                var appseverCommand = "";
                if (_azureSignalRConnectionString == null)
                {
                    appseverCommand = $"export useLocalSignalR=true; " +
                                      $"cd {appserverDirectory};" +
                                      $"dotnet exec AppServer.dll --urls=http://*:{_appserverPort}";
                }
                else
                {
                    appseverCommand = $"export Azure__SignalR__ConnectionString='{_azureSignalRConnectionString}';" +
                                      $"cd {appserverDirectory};" +
                                      $"dotnet exec AppServer.dll --urls=http://*:{_appserverPort}";
                }
                var masterCommand = $"dotnet exec {masterExecutablePath} -- " +
                                    $"--BenchmarkConfiguration=\"{_benchmarkConfigurationTargetPath}\"" +
                                    $"--SlaveList=\"{string.Join(',', _slaveList)}\"";
                var slaveCommand = $"dotnet exec {slaveExecutablePath} --HostName 0.0.0.0 --RpcPort {_rpcPort}";
                var appserverSshCommands = (from client in _remoteClients.AppserverSshClients
                                            select client.CreateCommand(appseverCommand.Replace('\\', '/'))).ToList();
                var masterSshCommand = _remoteClients.MasterSshClient.CreateCommand(masterCommand.Replace('\\', '/'));
                var slaveSshCommands = (from client in _remoteClients.SlaveSshClients
                                        select client.CreateCommand(slaveCommand.Replace('\\', '/'))).ToList();

                // Start app server
                Log.Information($"Start app server");
                var appserverAsyncResults = (from command in appserverSshCommands
                                             select command.BeginExecute(OnAsyncSshCommandComplete, command)).ToList();
                WaitAppserverStarted(appserverSshCommands);
                // Start slaves
                Log.Information($"Start slaves");
                var slaveAsyncResults = (from command in slaveSshCommands
                                         select command.BeginExecute(OnAsyncSshCommandComplete, command)).ToList();

                // Wait app server started
                Task.Delay(TimeSpan.FromSeconds(30)).Wait();

                // Start master
                Log.Information($"Start master");
                var masterResult = masterSshCommand.BeginExecute();
                using (var reader =
                   new StreamReader(masterSshCommand.OutputStream, Encoding.UTF8, true, 1024))
                {
                    while (!masterResult.IsCompleted || !reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        if (line != null)
                        {
                            Log.Information(line);
                        }
                    }
                }

                masterSshCommand.EndExecute(masterResult);

                CopyAppServerLog(appserverSshCommands);
            }
            finally
            {
                // Killall dotnet
                KillallDotnet();
            }
        }

        private void KillallDotnet()
        {
            void killDotnet(SshClient client)
            {
                var command = "killall dotnet";
                var sshCommand = client.CreateCommand(command);
                _ = sshCommand.Execute();
            }

            _remoteClients.AppserverSshClients.ToList().ForEach(client => killDotnet(client));
            killDotnet(_remoteClients.MasterSshClient);
            _remoteClients.SlaveSshClients.ToList().ForEach(client => killDotnet(client));
        }

        private void CopyExcutables(FileInfo appserverExecutable, FileInfo masterExecutable, FileInfo slaveExecutable)
        {
            Log.Information($"Copy executables to hosts...");

            // Copy executables
            var copyTasks = new List<Task>();
            copyTasks.AddRange(from client in _remoteClients.AppserverScpClients
                               select Task.Run(() => client.Upload(appserverExecutable, _appserverTargetPath)));
            copyTasks.Add(Task.Run(() => _remoteClients.MasterScpClient.Upload(masterExecutable, _masterTargetPath)));
            copyTasks.Add(Task.Run(() => _remoteClients.MasterScpClient.Upload(_benchmarkConfiguration, _benchmarkConfigurationTargetPath)));
            copyTasks.AddRange(from client in _remoteClients.SlaveScpClients
                                select Task.Run(() => client.Upload(slaveExecutable, _slaveTargetPath)));
            Task.WhenAll(copyTasks).Wait();
        }

        private (FileInfo appserverExecutable, FileInfo masterExecutable, FileInfo slaveExecutable) PubishExcutables()
        {
            Log.Information($"Generate executables...");
            var appserverExecutable = PubishExcutable(_appserverProject, _baseName);
            var masterExecutable = PubishExcutable(_masterProject, _baseName);
            var slaveExecutable = PubishExcutable(_slaveProject, _baseName);
            return (appserverExecutable, masterExecutable, slaveExecutable); 
        }

        private FileInfo PubishExcutable(string projectPath, string baseName)
        {
            Log.Information($"project path: {projectPath}");
            var publish = Command.Run("dotnet", $"publish -o {baseName}".Split(' '), o => o.WorkingDirectory(projectPath));
            publish.Wait();
            if (!publish.Result.Success) throw new Exception(publish.Result.StandardOutput);

            var zip = Command.Run("zip", new string[] { "-r", $"{baseName}.zip", $"{baseName}/" }, o => o.WorkingDirectory(projectPath));
            zip.Wait();
            if (!zip.Result.Success) throw new Exception(zip.Result.StandardOutput);

            return new FileInfo(Path.Combine(projectPath, $"{baseName}.zip"));
        }

        private void InstallZip(SshClient client)
        {
            var command = client.CreateCommand("sudo apt-get install -y zip");
            var result = command.Execute();
            if (command.Error != "")
            {
                Log.Error($"Install zip error on {client.ConnectionInfo.Host}: {result}, {command.Error}");
            }
        }

        private void Unzip(SshClient client, string targetPath)
        {
            SshCommand command = null;
            var directory = Path.GetDirectoryName(targetPath);
            var filenameWithoutExtension = Path.GetFileNameWithoutExtension(targetPath);
            var fileName = Path.GetFileName(targetPath);
            var ext = Path.GetExtension(targetPath);
            if (string.Equals("zip", ext, StringComparison.OrdinalIgnoreCase))
            {
                InstallZip(client);
                command = client.CreateCommand($"unzip -o -d {filenameWithoutExtension} {targetPath}");
            }
            else
            {
                command = client.CreateCommand($"cd {directory}; mkdir -p {filenameWithoutExtension}; tar zxvf {fileName} -C {directory}/{filenameWithoutExtension}");
            }
            var result = command.Execute();
            if (command.Error != "")
            {
                throw new Exception(command.Error);
            }
        }

        private void UnzipExecutables()
        {
            Log.Information($"Unzip executables");

            var tasks = new List<Task>();
            tasks.AddRange(from client in _remoteClients.AppserverSshClients
                           select Task.Run(() => Unzip(client, _appserverTargetPath)));
            tasks.Add(Task.Run(() => Unzip(_remoteClients.MasterSshClient, _masterTargetPath)));
            tasks.AddRange(from client in _remoteClients.SlaveSshClients
                           select Task.Run(() => Unzip(client, _slaveTargetPath)));
            Task.WhenAll(tasks).Wait();
        }
    }
}
