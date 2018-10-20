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
    class Program
    {
        public static async Task Main(string[] args)
        {
            var argsOption = ParseArgs(args);

            Util.CreateLogger(argsOption.LogDirectory, argsOption.LogName, argsOption.LogTarget);

            // Publish dlls
            var appserverExecutable = PubishExcutable(argsOption.AppserverProject);
            var masterExecutable = PubishExcutable(argsOption.MasterProject);
            var slaveExecutable = PubishExcutable(argsOption.SlaveProject);

            // Copy executables
            CopyExcutables(argsOption.Username, argsOption.Password,
                argsOption.AppServerHostname, argsOption.MasterHostname, argsOption.SlaveList,
                appserverExecutable, masterExecutable, slaveExecutable, 
                argsOption.AppserverTargetPath, argsOption.MasterTargetPath, argsOption.SlaveTargetPath);

            // Create Ssh clients
            (var appServerSshClient, var masterSshClient, var slaveSshClients) 
                = CreateSshClients(argsOption.Username, argsOption.Password, 
                argsOption.AppServerHostname, argsOption.MasterHostname, argsOption.SlaveList);

            // Connect to Vms
            appServerSshClient.Connect();
            masterSshClient.Connect();
            slaveSshClients.ToList().ForEach(client => client.Connect());

            // Run benchmark
            RunBenchmark(appServerSshClient, masterSshClient, slaveSshClients,
                 argsOption.AppserverTargetPath, argsOption.MasterTargetPath, argsOption.SlaveTargetPath);
        }

        private static void RunBenchmark(SshClient appServerSshClient, SshClient masterSshClient, IList<SshClient> slaveSshClients,
            string appserverTargetPath, string masterTargetPath, string slaveTargetPat)
        {
            var appseverCommand = $"dotnet exec {appserverTargetPath}";
            var masterCommand = $"dotnet exec {masterTargetPath}";
            var slaveCommand = $"dotnet exec {slaveTargetPat}";

            var appserverSshCommand = appServerSshClient.CreateCommand(appseverCommand);
            var masterSshCommand = masterSshClient.CreateCommand(masterCommand);
            var slaveSshCommands = (from client in slaveSshClients select client.CreateCommand(slaveCommand)).ToList();

            var appserverAsyncResult = appserverSshCommand.BeginExecute();
            var slaveAsyncResults = (from command in slaveSshCommands select command.BeginExecute()).ToList();

            var masterResult = masterSshCommand.Execute();

            _ = appserverSshCommand.EndExecute(appserverAsyncResult);
            _ = from i in Enumerable.Range(0, slaveCommand.Count())
                select slaveSshCommands[i].EndExecute(slaveAsyncResults[i]);
        }

        private static (SshClient, SshClient, IList<SshClient>) CreateSshClients(string username, string password,
            string appserverHostname, string masterHostname, IList<string> slaveHostnames)
        {
            return (new SshClient(appserverHostname, username, password),
                new SshClient(masterHostname, username, password), 
                (from slave in slaveHostnames select new SshClient(slave, username, password)).ToList());
        }

        private static void CopyExcutables(string username, string password, 
            string appserverHostname, string masterHostname, IList<string> slaveHostnames,
            FileInfo appserverExecutable, FileInfo masterExecutable, FileInfo slaveExecutable,
            string appserverTargetPath, string masterTargetPath, string slaveTargetPath)
        {
            // Copy executable to app server
            var appServerScpClient = new ScpClient(appserverHostname, username, password);
            appServerScpClient.Upload(appserverExecutable, appserverTargetPath);

            // Copy executable to master
            var masterScpClient = new ScpClient(masterHostname, username, password);
            masterScpClient.Upload(masterExecutable, masterTargetPath);

            // Copy excutable to slaves
            var slaveScpClients = from slave in slaveHostnames
                                  select new ScpClient(slave, username, password);
            foreach (var slaveScpClient in slaveScpClients) slaveScpClient.Upload(slaveExecutable, slaveTargetPath);
        }

        private static FileInfo PubishExcutable(string projectPath)
        {
            var baseName = "publish";
            _ = Command.Run("dotnet", $"publish -o {baseName}".Split(' '), o => o.WorkingDirectory(projectPath));
            _ = Command.Run("zip", $"-r publish.zip ./{baseName}/*".Split(' '), o => o.WorkingDirectory(projectPath));

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
                    throw new ArgumentException("Error in parsing arguments.");
                });
            return argsOption;
        }
    }
}
