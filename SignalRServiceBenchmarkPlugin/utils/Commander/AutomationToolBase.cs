using Medallion.Shell;
using Renci.SshNet;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Commander
{
    // Basic functionalities for automation tool
    public class AutomationToolBase
    {
        // Executable information
        protected readonly string _baseName = "publish";
        protected readonly string _appLogFileName = "appserver.log";
        // App server information
        protected readonly int _appserverPort;
        protected readonly string _azureSignalRConnectionString;
        protected readonly string _appserverLogDirPath;
        protected readonly bool _notStartAppServer;
        protected readonly bool _notStopAppServer;
        protected readonly IList<string> _appServerHostNameList;
        // Slave Information
        protected readonly int _rpcPort;
        protected readonly IList<string> _slaveList;
        protected readonly IList<string> _slaveHostNameList;

        // Local project path
        protected readonly string _appserverProject;
        protected readonly string _masterProject;
        protected readonly string _slaveProject;

        // Remote executable files
        protected readonly string _appserverTargetPath;
        protected readonly string _masterTargetPath;
        protected readonly string _slaveTargetPath;

        // Benchmark configuration
        protected readonly FileInfo _benchmarkConfiguration;

        // Remote benchmark configuration
        protected readonly string _benchmarkConfigurationTargetPath;

        protected readonly string _username;
        protected readonly string _password;
        protected readonly string _masterHostName;

        // Scp/Ssh clients
        protected RemoteClients _remoteClients;
        protected volatile int _startedAppServer;

        public AutomationToolBase(ArgsOption argOption)
        {
            // Initialize
            _appserverProject = argOption.AppserverProject;
            _appserverLogDirPath = argOption.AppserverLogDirectory;
            _notStartAppServer = argOption.NotStartAppServer == 1;
            _notStopAppServer = argOption.NotStopAppServer == 1;
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
            _username = argOption.Username;
            _password = argOption.Password;
            var appServerCount = argOption.AppServerHostnames.Count();
            var appServerCountInUse = argOption.AppServerCountInUse;
            _appServerHostNameList = argOption.AppServerHostnames?.Take(
                appServerCountInUse < appServerCount ?
                appServerCountInUse : appServerCount).ToList();
            // Create clients
            _masterHostName = argOption.MasterHostname;
            _slaveHostNameList = (from slave in argOption.SlaveList select slave.Split(':')[0]).ToList();

            _remoteClients = new RemoteClients();
            _remoteClients.CreateAll(_username, _password,
                _appServerHostNameList, _masterHostName, _slaveHostNameList);
        }

        protected string ConcatPathWithBaseName(string path)
        {
            return Path.Combine(Path.GetDirectoryName(path),
                        Path.GetFileNameWithoutExtension(path), _baseName);
        }

        protected string GenAppServerRemoteDirection()
        {
            return ConcatPathWithBaseName(_appserverTargetPath);
        }

        protected void OnAsyncSshCommandComplete(IAsyncResult result)
        {
            var command = (SshCommand)result.AsyncState;
            if (command.ExitStatus != 0)
            {
                Log.Error($"SshCommand '{command.CommandText}' occurs error: {command.Error}");
                throw new Exception(command.Error);
            }
        }

        protected void OnStartAppAsyncSshCommandComplete(IAsyncResult result)
        {
            OnAsyncSshCommandComplete(result);
            _startedAppServer++;
        }

        protected string GetRemoteAppServerLogPath()
        {
            var appserverDirectory = GenAppServerRemoteDirection();
            var appLogFilePath = Path.Combine(appserverDirectory, _appLogFileName);
            return appLogFilePath;
        }

        protected string GetLocalAppServerLogPath(string tag)
        {
            var fileName = $"{tag}_{_appLogFileName}";
            return Path.Combine(_appserverLogDirPath, fileName);
        }

        protected void WaitAppServerStarted()
        {
            var recheckTimeout = 600;
            var keyWords = "HttpConnection Started";
            CreateAppServerLogDirIfNotExist();
            var remoteAppLogFilePath = GetRemoteAppServerLogPath();
            string content = null;
            foreach (var client in _remoteClients.AppserverScpClients)
            {
                var host = client.ConnectionInfo.Host;
                var localAppServerLogPath = GetLocalAppServerLogPath(host);
                var recheck = 0;
                while (recheck < recheckTimeout)
                {
                    client.Download(remoteAppLogFilePath, new FileInfo(localAppServerLogPath));
                    using (StreamReader sr = new StreamReader(localAppServerLogPath))
                    {
                        content = sr.ReadToEnd();
                        if (content.Contains(keyWords))
                        {
                            Log.Information($"app server {host} started!");
                            break;
                        }
                    }
                    recheck++;
                    Log.Information($"waiting for appserver {host} starting...");
                    Task.Delay(TimeSpan.FromSeconds(1)).Wait();
                }
                if (recheck == recheckTimeout)
                {
                    Log.Error($"Fail to start server {host}!!!");
                    if (content != null)
                    {
                        Log.Information($"log content: {content}");
                    }
                }
            }
        }

        protected void CreateAppServerLogDirIfNotExist()
        {
            if (!String.IsNullOrEmpty(_appserverLogDirPath) && !Directory.Exists(_appserverLogDirPath))
            {
                Directory.CreateDirectory(_appserverLogDirPath);
            }
        }

        protected void CopyAppServerLog()
        {
            CreateAppServerLogDirIfNotExist();
            var remoteAppLogFilePath = GetRemoteAppServerLogPath();
            foreach (var client in _remoteClients.AppserverScpClients)
            {
                var host = client.ConnectionInfo.Host;
                var localAppServerLogPath = GetLocalAppServerLogPath(host);
                client.Download(remoteAppLogFilePath, new FileInfo(localAppServerLogPath));
                // zip the applog because it may be big
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var zip = Command.Run("zip", new string[] { "-r", $"{localAppServerLogPath}.tgz", $"{localAppServerLogPath}" },
                    o => o.WorkingDirectory(_appserverLogDirPath));
                    zip.Wait();
                    if (!zip.Result.Success)
                    {
                        Log.Error(zip.Result.StandardOutput);
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    var tar = Command.Run("tar", new string[] { "-C", _appserverLogDirPath, "zcvf", $"{localAppServerLogPath}.tgz", $"{host}_{_appLogFileName}" },
                    o => o.WorkingDirectory(_appserverLogDirPath));
                    tar.Wait();
                    if (!tar.Result.Success)
                    {
                        Log.Error(tar.Result.StandardOutput);
                    }
                    else
                    {
                        Command.Run("rm", new string[] { $"{localAppServerLogPath}" }, o => o.WorkingDirectory(_appserverLogDirPath));
                    }
                }
            }
        }

        protected void RetriableUploadFile(string host, string srcPath, string dstPath)
        {
            int code, retry = 5;
            string error;
            for (var i = 0; i < retry; i++)
            {
                (code, error) = BashUtils.UploadFileToRemote(host, _username, _password, srcPath, dstPath);
                if (code == 0)
                {
                    break;
                }
                else
                {
                    Log.Error($"will retry to upload file from {srcPath} to {dstPath} for {error}");
                }
            }
        }

        protected void CreateDirIfNotExist(FileInfo appserverExecutable, FileInfo masterExecutable, FileInfo slaveExecutable)
        {
            string GetDir(string path) => Path.GetDirectoryName(path).Replace('\\', '/');

            // Make sure the remote path is available
            var makeDirTasks = new List<Task>();
            makeDirTasks.Add(Task.Run(() => _remoteClients.MasterSshClient.CreateCommand("mkdir -p " + GetDir(_masterTargetPath))));
            if (!_notStartAppServer)
            {
                makeDirTasks.AddRange(from client in _remoteClients.AppserverSshClients
                                      select Task.Run(() => client.CreateCommand(
                                          "mkdir -p " +
                                          GetDir(_appserverTargetPath)).Execute()));
            }

            makeDirTasks.AddRange(from client in _remoteClients.SlaveSshClients
                                  select Task.Run(() => client.CreateCommand(
                                      "mkdir -p " +
                                      GetDir(_slaveTargetPath)).Execute()));

            Task.WhenAll(makeDirTasks).Wait();
        }

        protected void InstallZip(SshClient client)
        {
            var command = client.CreateCommand("sudo apt-get install -y zip");
            var result = command.Execute();
            if (command.Error != "")
            {
                Log.Error($"Install zip error on {client.ConnectionInfo.Host}: {result}, {command.Error}");
            }
        }

        protected void Unzip(SshClient client, string targetPath)
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

        protected void UnzipExecutables()
        {
            Log.Information($"Unzip executables");

            var tasks = new List<Task>();
            if (!_notStartAppServer)
            {
                tasks.AddRange(from client in _remoteClients.AppserverSshClients
                               select Task.Run(() => Unzip(client, _appserverTargetPath)));
            }
            tasks.Add(Task.Run(() => Unzip(_remoteClients.MasterSshClient, _masterTargetPath)));
            tasks.AddRange(from client in _remoteClients.SlaveSshClients
                           select Task.Run(() => Unzip(client, _slaveTargetPath)));
            Task.WhenAll(tasks).Wait();
        }
    }
}
