using Common;
using Medallion.Shell;
using Renci.SshNet;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Commander
{
    public class AutomationTool : AutomationToolBase
    {
        public AutomationTool(ArgsOption argOption) : base(argOption)
        {
        }

        // Only for development
        public void StartDev()
        {
            try
            {
                // Clients connect to host
                _remoteClients.ConnectAll();

                FileInfo appserverExecutable = null, masterExecutable, agentExecutable;
                if (!_notStartAppServer)
                {
                    appserverExecutable = File.Exists($"{_appserverProject}/{_baseName}.tgz") ?
                        new FileInfo($"{_appserverProject}/{_baseName}.tgz") :
                        new FileInfo($"{_appserverProject}/{_baseName}.zip");
                }
                masterExecutable = File.Exists($"{_masterProject}/{_baseName}.tgz") ?
                    new FileInfo($"{_masterProject}/{_baseName}.tgz") :
                    new FileInfo($"{_masterProject}/{_baseName}.zip");
                agentExecutable = File.Exists($"{_agentProject}/{_baseName}.tgz") ?
                    new FileInfo($"{_agentProject}/{_baseName}.tgz") :
                    new FileInfo($"{_agentProject}/{_baseName}.zip");

                // Copy executables and configurations
                CopyExecutables(appserverExecutable, masterExecutable, agentExecutable);

                // Unzip packages
                UnzipExecutables();

                // Run benchmark
                RunBenchmark();
            }
            catch (Exception e)
            {
                Log.Error($"Automation tool stops for {e}");
            }
            finally
            {
                // Disconnect and dispose
                _remoteClients.DestroyAll();
            }
        }

        private string GenAppServerRemoteScriptContent(bool relaunch=true)
        {
            var appserverDirectory = GenAppServerRemoteDirection();
            var appserverScript = "";
            var cmdPrefix = "dotnet exec appserver.dll";
            var appLogFile = _appLogFileName;
            if (_azureSignalRConnectionString == null)
            {
                if (relaunch)
                {
                    appserverScript = $@"
#!/bin/bash
    isRun=`ps axu|grep '{cmdPrefix}'|wc -l`
    if [ $isRun -gt 1 ]
    then
        killall dotnet
    fi
    export useLocalSignalR=true
    cd {appserverDirectory}
    {cmdPrefix} --urls=http://*:{_appserverPort} > {appLogFile}
";
                }
                else
                {
                    appserverScript = $@"
#!/bin/bash
isRun=`ps axu|grep '{cmdPrefix}'|wc -l`
if [ $isRun -eq 1 ]
then
    export useLocalSignalR=true
    cd {appserverDirectory}
    {cmdPrefix} --urls=http://*:{_appserverPort} > {appLogFile}
else
    echo 'AppServer has started'
fi
";
                }
            }
            else
            {
                if (relaunch)
                {
                    appserverScript = $@"
#!/bin/bash
    isRun=`ps axu|grep '{cmdPrefix}'|wc -l`
    if [ $isRun -gt 1 ]
    then
        killall dotnet
    fi
    cd {appserverDirectory}
    export Azure__SignalR__ConnectionString=""{_azureSignalRConnectionString}""
    {cmdPrefix} --urls=http://*:{_appserverPort} > {appLogFile}
";
                }
                else
                {
                    appserverScript = $@"
#!/bin/bash
    isRun=`ps axu|grep '{cmdPrefix}'|wc -l`
    if [ $isRun -eq 1 ]
    then
        cd {appserverDirectory}
        export Azure__SignalR__ConnectionString=""{_azureSignalRConnectionString}""
        {cmdPrefix} --urls=http://*:{_appserverPort} > {appLogFile}
    else
        echo 'AppServer has started'
    fi
";
                }
            }
            return appserverScript;
        }

        private void StartAppServer(bool relaunch=true)
        {
            var appserverScript = GenAppServerRemoteScriptContent(relaunch);
            // Copy script to start appserver to every app server VM
            var scriptName = "startAppServer.sh";
            var startAppServerScript = new FileInfo(scriptName);
            using (var writer = new StreamWriter(startAppServerScript.FullName, false))
            {
                writer.Write(appserverScript);
            }
            var appserverDirectory = GenAppServerRemoteDirection();
            var remoteScriptPath = Path.Combine(appserverDirectory, scriptName);

            var startAppServerTasks = new List<Task>();
            startAppServerTasks.AddRange(from client in _remoteClients.AppserverScpClients
                         select Task.Run(() =>
                         {
                             try
                             {
                                 //client.Upload(startAppServerScript, remoteScriptPath);
                                 var clientHost = client.ConnectionInfo.Host;
                                 RetriableUploadFile(clientHost, startAppServerScript.FullName, remoteScriptPath);
                                 Log.Information($"Successfully upload {startAppServerScript} to {client.ConnectionInfo.Host}/{remoteScriptPath}");
                             }
                             catch (Exception e)
                             {
                                 Log.Error($"Fail to upload startAppServer script: {e.Message}");
                             }
                         }));
            Task.WhenAll(startAppServerTasks).Wait();
            // launch those scripts
            var launchAppserverCmd = $"cd {appserverDirectory}; chmod +x {scriptName}; ./{scriptName}";
            var appserverSshCommands = (from client in _remoteClients.AppserverSshClients
                                        select client.CreateCommand(launchAppserverCmd.Replace('\\', '/'))).ToList();
            var appserverAsyncResults = (from command in appserverSshCommands
                                         select command.BeginExecute(OnStartAppAsyncSshCommandComplete, command)).ToList();
            Util.TimeoutCheckedTask(Task.Run(async ()=>
            {
                while (_startedAppServer < _remoteClients.AppserverSshClients.Count)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
                Log.Information("All app server launched");
            }), 60000);
            Task.Delay(TimeSpan.FromSeconds(60)).Wait();
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

                var agentDirectory = ConcatPathWithBaseName(_agentTargetPath);
                var masterDirectory = ConcatPathWithBaseName(_masterTargetPath);
                var masterExecutablePath = Path.Combine(ConcatPathWithBaseName(_masterTargetPath), "master.dll");
                var agentExecutablePath = Path.Combine(ConcatPathWithBaseName(_agentTargetPath), "agent.dll");

                var masterCommand = $"cd {masterDirectory}; " +
                                    $"dotnet exec {masterExecutablePath} -- " +
                                    $"--BenchmarkConfiguration=\"{_benchmarkConfigurationTargetPath}\" " +
                                    $"--AgentList=\"{string.Join(',', _agentList)}\"";
                var agentCommand = $"cd {agentDirectory}; dotnet exec {agentExecutablePath} --HostName 0.0.0.0 --RpcPort {_rpcPort}";

                var masterSshCommand = _remoteClients.MasterSshClient.CreateCommand(masterCommand.Replace('\\', '/'));

                // Start app server
                if (!_notStartAppServer)
                {
                    Log.Information($"Start app server");
                    StartAppServer(!_notStopAppServer);
                    WaitAppServerStarted();
                }
                else
                {
                    Log.Information("Do not start appserver !");
                }
                // Start agents
                // Check connection status
                foreach (var sshClient in _remoteClients.AgentSshClients)
                {
                    if (!sshClient.IsConnected)
                    {
                        var endpoint = $"{sshClient.ConnectionInfo.Host}:{sshClient.ConnectionInfo.Port}";
                        Log.Warning($"a agent ssh {endpoint} dropped, and try reconnect");
                        try
                        {
                            sshClient.Connect();
                        }
                        catch (Exception e)
                        {
                            Log.Information($"reconnect failure: {e.Message}");
                            throw;
                        }
                    }
                }
                Log.Information("All agents connected");
                var agentSshCommands = (from client in _remoteClients.AgentSshClients
                                        select client.CreateCommand(agentCommand.Replace('\\', '/'))).ToList();
                Log.Information($"Start agents");
                var agentAsyncResults = (from command in agentSshCommands
                                         select command.BeginExecute(OnAsyncSshCommandComplete, command)).ToList();

                // Start master
                Log.Information($"Start master");
                var masterResult = masterSshCommand.BeginExecute();
                using (var reader =
                   new StreamReader(masterSshCommand.OutputStream, Encoding.UTF8, true, 4096, true))
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

                var result = masterSshCommand.EndExecute(masterResult);
                var exitStatus = masterSshCommand.ExitStatus;
                var masterError = masterSshCommand.Error;
                Log.Information($"Master execute result: {result}, exit status: {exitStatus}, error: '{masterError}'");
                if (!_notStartAppServer)
                {
                    CopyAppServerLog();
                }
            }
            catch (Exception e)
            {
                Log.Error($"unhandled exception {e}");
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
            if (!_notStartAppServer && !_notStopAppServer)
            {
                _remoteClients.AppserverSshClients.ToList().ForEach(client => killDotnet(client));
            }
            killDotnet(_remoteClients.MasterSshClient);
            _remoteClients.AgentSshClients.ToList().ForEach(client => killDotnet(client));
        }

        private void CopyExecutables(FileInfo appserverExecutable, FileInfo masterExecutable, FileInfo agentExecutable)
        {
            Log.Information($"Copy executables to hosts...");

            CreateDirIfNotExist(appserverExecutable, masterExecutable, agentExecutable);
            if (!_notStartAppServer)
            {
                foreach (var appserver in _appServerHostNameList)
                {
                    var srcFile = appserverExecutable.FullName;
                    var dstFile = _appserverTargetPath;
                    RetriableUploadFile(appserver, srcFile, dstFile);
                }
            }

            RetriableUploadFile(_masterHostName, masterExecutable.FullName, _masterTargetPath);
            RetriableUploadFile(_masterHostName, _benchmarkConfiguration.FullName, _benchmarkConfigurationTargetPath);
            foreach (var agentHost in _agentHostNameList)
            {
                var srcFile = agentExecutable.FullName;
                var dstFile = _agentTargetPath;
                RetriableUploadFile(agentHost, srcFile, dstFile);
            }
        }

        // The ScpClient of SSH.NET has bug https://github.com/sshnet/SSH.NET/issues/516. We cannot use it until it is fixed.
        private void CopyExecutables2(FileInfo appserverExecutable, FileInfo masterExecutable, FileInfo agentExecutable)
        {
            Log.Information($"Copy executables to hosts...");

            CreateDirIfNotExist(appserverExecutable, masterExecutable, agentExecutable);
            // Copy executables
            var copyTasks = new List<Task>();
            if (!_notStartAppServer)
            {
                copyTasks.AddRange(from client in _remoteClients.AppserverScpClients
                                   select Task.Run(() =>
                                   client.Upload(appserverExecutable, _appserverTargetPath)));
            }
            copyTasks.Add(Task.Run(() => _remoteClients.MasterScpClient.Upload(masterExecutable, _masterTargetPath)));
            copyTasks.Add(Task.Run(() => _remoteClients.MasterScpClient.Upload(_benchmarkConfiguration, _benchmarkConfigurationTargetPath)));
            copyTasks.AddRange(from client in _remoteClients.AgentScpClients
                                select Task.Run(() => client.Upload(agentExecutable, _agentTargetPath)));
            Task.WhenAll(copyTasks).Wait();
        }

        // TODO. publish executables do not work since we have to build through script instead of raw dotnet command
        private (FileInfo appserverExecutable, FileInfo masterExecutable, FileInfo agentExecutable) PubishExcutables()
        {
            Log.Information($"Generate executables...");

            FileInfo appserverExecutable = null;
            if (!_notStartAppServer)
            {
                appserverExecutable = PubishExcutable(_appserverProject, _baseName);
            }
            var masterExecutable = PubishExcutable(_masterProject, _baseName);
            var agentExecutable = PubishExcutable(_agentProject, _baseName);
            return (appserverExecutable, masterExecutable, agentExecutable); 
        }

        private FileInfo PubishExcutable(string projectPath, string baseName)
        {
            Log.Information($"project path: {projectPath}");
            var publish = Command.Run("dotnet", $"publish -o {baseName}".Split(' '), o => o.WorkingDirectory(projectPath));
            publish.Wait();
            if (!publish.Result.Success) throw new Exception(publish.Result.StandardOutput);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var zip = Command.Run("zip", new string[] { "-r", $"{baseName}.zip", $"{baseName}/" }, o => o.WorkingDirectory(projectPath));
                zip.Wait();
                if (!zip.Result.Success) throw new Exception(zip.Result.StandardOutput);
                return new FileInfo(Path.Combine(projectPath, $"{baseName}.zip"));
            }
            else
            {
                var tar = Command.Run("tar", new string[] { "zcvf", $"{baseName}.tgz", $"{baseName}/" }, o => o.WorkingDirectory(projectPath));
                tar.Wait();
                if (!tar.Result.Success) throw new Exception(tar.Result.StandardOutput);
                return new FileInfo(Path.Combine(projectPath, $"{baseName}.tgz"));
            }
        }
    }
}
