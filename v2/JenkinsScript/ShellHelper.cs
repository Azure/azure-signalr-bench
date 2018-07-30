using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace JenkinsScript
{
    class ShellHelper
    {
        public static void HandleResult (int errCode, string result)
        {
            if (errCode != 0)
            {
                Util.Log ($"ERR {errCode}: {result}");
                Environment.Exit (1);
            }
            return;
        }

        public static (int, string) Bash (string cmd, bool wait = true, bool handleRes = false)
        {
            var escapedArgs = cmd.Replace ("\"", "\\\"");

            var process = new Process ()
            {
                StartInfo = new ProcessStartInfo
                {
                FileName = "/bin/bash",
                Arguments = $"-c \"{escapedArgs}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                }
            };
            process.Start ();
            var result = "";
            var errCode = 0;
            if (wait == true) result = process.StandardOutput.ReadToEnd ();
            if (wait == true) process.WaitForExit ();
            if (wait == true) errCode = process.ExitCode;

            if (handleRes == true)
            {
                HandleResult (errCode, result);
            }

            return (errCode, result);
        }

        public static (int, string) RemoteBash (string user, string host, int port, string password, string cmd, bool wait = true, bool handleRes = false, int retry = 1)
        {

            int errCode = 0;
            string result = "";
            for (var i = 0; i < retry; i++)
            {
                if (host.IndexOf ("localhost") >= 0 || host.IndexOf ("127.0.0.1") >= 0) return Bash (cmd, wait);
                Util.Log ($"password: {password}");
                Util.Log ($"port: {port}");
                Util.Log ($"host: {host}");
                Util.Log ($"cmd: {cmd}");
                string sshPassCmd = $"sshpass -p {password} ssh -p {port} -o StrictHostKeyChecking=no {user}@{host} \"{cmd}\"";
                Util.Log ($"SSH Pass Cmd: {sshPassCmd}");
                (errCode, result) = Bash (sshPassCmd, wait : wait, handleRes : retry > 1 && i < retry - 1 ? false : handleRes);
                if (errCode == 0) break;
                Util.Log ($"retry {i+1}th time");
                Task.Delay (TimeSpan.FromSeconds (1)).Wait ();
            }

            return (errCode, result);
        }

        public static (int, string) KillAllDotnetProcess (List<string> hosts, AgentConfig agentConfig, ArgsOption argsOption, string repoRoot = "/home/wanl/signalr_auto_test_framework")
        {
            var errCode = 0;
            var result = "";
            var cmd = "";

            hosts.ForEach (host =>
            {
                cmd = $"killall dotnet || true";
                if (host.Contains ("localhost") || host.Contains ("127.0.0.1")) { }
                else if (host == agentConfig.Master) // todo: move rpc master to another vm
                {
                    Util.Log ($"CMD: {agentConfig.User}@{host}: {cmd}");
                    (errCode, result) = ShellHelper.RemoteBash (agentConfig.User, host, agentConfig.SshPort, agentConfig.Password, cmd);
                }
                else
                {
                    Util.Log ($"CMD: {agentConfig.User}@{host}: {cmd}");
                    (errCode, result) = ShellHelper.RemoteBash (agentConfig.User, host, agentConfig.SshPort, agentConfig.Password, cmd);
                }
                if (errCode != 0) return;
            });

            if (errCode != 0)
            {
                Util.Log ($"ERR {errCode}: {result}");
                Environment.Exit (1);
            }

            return (errCode, result);
        }

        public static (int, string) GitCloneRepo (List<string> hosts, AgentConfig agentConfig, string commit = "", string branch = "origin/master", string repoRoot = "/home/wanl/signalr_auto_test_framework")
        {
            var errCode = 0;
            var result = "";

            var tasks = new List<Task> ();

            hosts.ForEach (host =>
            {
                tasks.Add (Task.Run (() =>
                {
                    var errCodeInner = 0;
                    var resultInner = "";
                    var cmdInner = $"rm -rf {repoRoot}; git clone {agentConfig.Repo} {repoRoot}; "; //TODO
                    cmdInner += $"cd {repoRoot};";
                    cmdInner += $"git checkout {branch};";
                    cmdInner += $"git reset --hard {commit};";
                    cmdInner += $" cd ~ ;";
                    Util.Log ($"CMD: {agentConfig.User}@{host}: {cmdInner}");
                    if (host == agentConfig.Master) { }
                    else (errCodeInner, resultInner) = ShellHelper.RemoteBash (agentConfig.User, host, agentConfig.SshPort, agentConfig.Password, cmdInner);
                    if (errCodeInner != 0)
                    {
                        errCode = errCodeInner;
                        result = resultInner;
                    }
                }));
            });

            Task.WhenAll (tasks).Wait ();

            if (errCode != 0)
            {
                Util.Log ($"ERR {errCode}: {result}");
                Environment.Exit (1);
            }

            return (errCode, result);
        }

        public static (int, string) StartAppServer (List<string> hosts, AgentConfig agentConfig, string azureSignalrConnectionString,
            string serviceType, string transportType, string hubProtocol, string scenario, int connection,
            string useLocalSingalR = "false", string repoRoot = "/home/wanl/signalr_auto_test_framework")
        {
            var errCode = 0;
            var result = "";
            var cmd = "";

            cmd = $"cd {repoRoot}/signalr_bench/AppServer/; " +
                $"export Azure__SignalR__ConnectionString='{azureSignalrConnectionString}'; " +
                $"export useLocalSignalR={useLocalSingalR}; " +
                $"mkdir log/{Environment.GetEnvironmentVariable("result_root")}/; dotnet run > log/{Environment.GetEnvironmentVariable("result_root")}/log_appserver_{serviceType}_{transportType}_{hubProtocol}_{scenario}_{connection}.txt";
            Util.Log ($"{agentConfig.User}@{agentConfig.AppServer}: {cmd}");
            (errCode, result) = ShellHelper.RemoteBash (agentConfig.User, agentConfig.AppServer, agentConfig.SshPort, agentConfig.Password, cmd, wait : false);

            if (errCode != 0)
            {
                Util.Log ($"ERR {errCode}: {result}");
                Environment.Exit (1);
            }

            return (errCode, result);

        }

        public static (int, string) StartRpcSlaves (AgentConfig agentConfig, ArgsOption argsOption,
            string serviceType, string transportType, string hubProtocol, string scenario, int connection, string repoRoot = "/home/wanl/signalr_auto_test_framework")
        {
            var errCode = 0;
            var result = "";
            var cmd = "";

            agentConfig.Slaves.ForEach (host =>
            {
                cmd = $"cd {repoRoot}/signalr_bench/Rpc/Bench.Server/; mkdir log/{Environment.GetEnvironmentVariable("result_root")}/; dotnet run -- --rpcPort {agentConfig.RpcPort} -d 0.0.0.0 > log/{Environment.GetEnvironmentVariable("result_root")}/log_rpcslave_{serviceType}_{transportType}_{hubProtocol}_{scenario}_{connection}.txt";
                Util.Log ($"CMD: {agentConfig.User}@{host}: {cmd}");
                (errCode, result) = ShellHelper.RemoteBash (agentConfig.User, host, agentConfig.SshPort, agentConfig.Password, cmd, wait : false);
                if (errCode != 0) return;
            });
            if (errCode != 0)
            {
                Util.Log ($"ERR {errCode}: {result}");
                Environment.Exit (1);
            }

            return (errCode, result);

        }

        public static (int, string) StartRpcMaster (AgentConfig agentConfig,
            ArgsOption argsOption, string serviceType, string transportType, string hubProtocol, string scenario,
            int connection, int duration, int interval, string pipeLine,
            int mixEchoConnection, int mixBroadcastConnection, int mixGroupConnection, string mixGroupName,
            int groupNum,
            string serverUrl, string repoRoot = "/home/wanl/signalr_auto_test_framework")
        {
            Util.Log ($"service type: {serviceType}, transport type: {transportType}, hub protocol: {hubProtocol}, scenario: {scenario}");
            var errCode = 0;
            var result = "";
            var cmd = "";

            var maxRetry = 1;
            var slaveList = "";

            for (var i = 0; i < agentConfig.Slaves.Count; i++)
            {
                slaveList += agentConfig.Slaves[i];
                if (i < agentConfig.Slaves.Count - 1)
                    slaveList += ";";
            }

            for (var i = 0; i < 1; i++)
            {
                var clear = "false";
                var outputCounterDir = "";
                var outputCounterFile = "";

                cmd = $"cd {repoRoot}/signalr_bench/Rpc/Bench.Client/; ";
                if (scenario == "echo" || scenario == "broadcast")
                    outputCounterDir = $"{repoRoot}/signalr_bench/Report/public/results/{Environment.GetEnvironmentVariable("result_root")}/{serviceType}_{transportType}_{hubProtocol}_{scenario}_{connection}_{groupNum}/";
                else if (scenario == "group")
                    outputCounterDir = $"{repoRoot}/signalr_bench/Report/public/results/{Environment.GetEnvironmentVariable("result_root")}/{serviceType}_{transportType}_{hubProtocol}_{scenario}_{connection}_{groupNum}/";

                outputCounterFile = outputCounterDir + $"counters.txt";

                cmd += $"rm -rf {outputCounterFile} || true;";

                cmd += $" mkdir log/{Environment.GetEnvironmentVariable("result_root")}/; ";

                cmd += $"dotnet build; dotnet run -- " +
                    $"--rpcPort 5555 " +
                    $"--duration {duration} --connections {connection} --interval {interval} --slaves {agentConfig.Slaves.Count} --serverUrl 'http://{serverUrl}:5050/signalrbench' --pipeLine '{string.Join(";", pipeLine)}' " +
                    $"-v {serviceType} -t {transportType} -p {hubProtocol} -s {scenario} " +
                    $" --slaveList '{slaveList}' " +
                    $" --retry {0} " +
                    $" --clear {clear} " +
                    $" --mixEchoConnection  {mixEchoConnection} " +
                    $" --mixBroadcastConnection  {mixBroadcastConnection} " +
                    $" --mixGroupConnection  {mixGroupConnection} " +
                    $" --mixGroupName  {mixGroupName} " +
                    $" --concurrentConnection 1 " +
                    $" --groupConnection {connection} " +
                    $" --groupNum {groupNum} " +
                    $" -o '{outputCounterFile}' > log/{Environment.GetEnvironmentVariable("result_root")}/log_rpcmaster_{serviceType}_{transportType}_{hubProtocol}_{scenario}_{connection}.txt";

                Util.Log ($"CMD: {agentConfig.User}@{agentConfig.Master}: {cmd}");
                (errCode, result) = ShellHelper.RemoteBash (agentConfig.User, agentConfig.Master, agentConfig.SshPort, agentConfig.Password, cmd);
                if (errCode == 0) break;
                Util.Log ($"retry {i}th time");

                if (errCode != 0)
                {
                    Util.Log ($"ERR {errCode}: {result}");
                }
            }

            return (errCode, result);

        }

        public static (int, string) CreateSignalrService (ArgsOption argsOption, int unitCount)
        {
            var errCode = 0;
            var result = "";
            var cmd = "";

            var content = AzureBlobReader.ReadBlob ("SignalrConfigFileName");
            Console.WriteLine ($"content: {content}");
            var config = AzureBlobReader.ParseYaml<SignalrConfig> (content);

            // login to azure
            cmd = $"az login --service-principal --username {config.AppId} --password {config.Password} --tenant {config.Tenant}";
            Util.Log ($"CMD: signalr service: az login");
            (errCode, result) = ShellHelper.Bash (cmd, handleRes : true);

            // change subscription
            cmd = $"az account set --subscription {config.Subscription}";
            Util.Log ($"CMD: az account set --subscription");
            (errCode, result) = ShellHelper.Bash (cmd, handleRes : true);

            // var groupName = Util.GenResourceGroupName(config.BaseName);
            // var srName = Util.GenSignalRServiceName(config.BaseName);

            var rnd = new Random ();
            var SrRndNum = (rnd.Next (10000) * rnd.Next (10000)).ToString ();

            var groupName = config.BaseName + "Group";
            var srName = config.BaseName + SrRndNum + "SR";

            cmd = $"  az extension add -n signalr || true";
            Util.Log ($"CMD: signalr service: {cmd}");
            (errCode, result) = ShellHelper.Bash (cmd, handleRes : true);

            // create resource group
            cmd = $"  az group create --name {groupName} --location {config.Location}";
            Util.Log ($"CMD: signalr service: {cmd}");
            (errCode, result) = ShellHelper.Bash (cmd, handleRes : true);

            //create signalr service
            cmd = $"az signalr create --name {srName} --resource-group {groupName}  --sku {config.Sku} --unit-count {unitCount} --query hostName -o tsv";
            Util.Log ($"CMD: signalr service: {cmd}");
            (errCode, result) = ShellHelper.Bash (cmd, handleRes : true);

            var signalrHostName = result;
            Console.WriteLine ($"signalrHostName: {signalrHostName}");

            // get access key
            cmd = $"az signalr key list --name {srName} --resource-group {groupName} --query primaryKey -o tsv";
            Util.Log ($"CMD: signalr service: {cmd}");
            (errCode, result) = ShellHelper.Bash (cmd, handleRes : true);
            var signalrPrimaryKey = result;
            Console.WriteLine ($"signalrPrimaryKey: {signalrPrimaryKey}");

            // combine to connection string
            signalrHostName = signalrHostName.Substring (0, signalrHostName.Length - 1);
            signalrPrimaryKey = signalrPrimaryKey.Substring (0, signalrPrimaryKey.Length - 1);
            var connectionString = $"Endpoint=https://{signalrHostName};AccessKey={signalrPrimaryKey};";
            Console.WriteLine ($"connection string: {connectionString}");
            ShellHelper.Bash ($"export AzureSignalRConnectionString='{connectionString}'", handleRes : true);
            return (errCode, connectionString);
        }

        public static (int, string) DeleteSignalr (ArgsOption args)
        {
            var errCode = 0;
            var result = "";
            var cmd = "";

            var content = AzureBlobReader.ReadBlob ("SignalrConfigFileName");
            var config = AzureBlobReader.ParseYaml<SignalrConfig> (content);

            var groupName = config.BaseName + "Group";

            // login to azure
            cmd = $"az login --service-principal --username {config.AppId} --password {config.Password} --tenant {config.Tenant}";
            Util.Log ($"CMD: signalr service: logint azure");
            (errCode, result) = ShellHelper.Bash (cmd, handleRes : true);

            // delete resource group
            cmd = $"az group delete --name {groupName} --yes";
            Util.Log ($"CMD: signalr service: {cmd}");
            (errCode, result) = ShellHelper.Bash (cmd, handleRes : true);

            return (errCode, result);
        }

    }

}