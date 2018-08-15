using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace JenkinsScript
{
    class ShellHelper
    {
        public static void HandleResult(int errCode, string result)
        {
            if (errCode != 0)
            {
                Util.Log($"ERR {errCode}: {result}");
                Environment.Exit(1);
            }
            return;
        }

        public static(int, string) Bash(string cmd, bool wait = true, bool handleRes = false)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");

            var process = new Process()
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
            process.Start();
            var result = "";
            var errCode = 0;
            if (wait == true) result = process.StandardOutput.ReadToEnd();
            if (wait == true) process.WaitForExit();
            if (wait == true) errCode = process.ExitCode;

            if (handleRes == true)
            {
                HandleResult(errCode, result);
            }

            return (errCode, result);
        }

        public static(int, string) ScpDirecotryLocalToRemote(string user, string host, string password, string src, string dst)
        {
            int errCode = 0;
            string result = "";
            string cmd = $"sshpass -p {password} scp -r -o StrictHostKeyChecking=no {src} {user}@{host}:{dst}";
            Util.Log($"scp cmd: {cmd}");
            (errCode, result) = Bash(cmd, wait : true, handleRes : true);
            return (errCode, result);
        }

        public static(int, string) ScpFileLocalToRemote(string user, string host, string password, string srcFile, string dstDir)
        {
            int errCode = 0;
            string result = "";
            string cmd = $"sshpass -p {password} scp -o StrictHostKeyChecking=no {srcFile} {user}@{host}:{dstDir}";
            (errCode, result) = Bash(cmd, wait : true, handleRes : true);
            return (errCode, result);
        }

        public static(int, string) ScpDirecotryRemoteToLocal(string user, string host, string password, string src, string dst)
        {
            int errCode = 0;
            string result = "";
            string cmd = $"sshpass -p {password} scp -r -o StrictHostKeyChecking=no {user}@{host}:{src} {dst}";
            (errCode, result) = Bash(cmd, wait : true, handleRes : true);
            return (errCode, result);
        }

        public static(int, string) RemoteBash(string user, string host, int port, string password, string cmd, bool wait = true, bool handleRes = false, int retry = 1)
        {

            int errCode = 0;
            string result = "";
            for (var i = 0; i < retry; i++)
            {
                if (host.IndexOf("localhost") >= 0 || host.IndexOf("127.0.0.1") >= 0) return Bash(cmd, wait);
                string sshPassCmd = $"echo \"\" > /home/wanl/.ssh/known_hosts; sshpass -p {password} ssh -p {port} -o StrictHostKeyChecking=no {user}@{host} \"{cmd}\"";
                (errCode, result) = Bash(sshPassCmd, wait : wait, handleRes : retry > 1 && i < retry - 1 ? false : handleRes);
                if (errCode == 0) break;
                Util.Log($"retry {i+1}th time");
                Task.Delay(TimeSpan.FromSeconds(1)).Wait();
            }

            return (errCode, result);
        }

        public static(int, string) KillAllDotnetProcess(List<string> hosts, string repoUrl, string user, string password, int sshPort, string repoRoot = "/home/wanl/signalr_auto_test_framework")
        {
            var errCode = 0;
            var result = "";
            var cmd = "";

            hosts.ForEach(host =>
            {
                cmd = $"killall dotnet || true";
                if (host.Contains("localhost") || host.Contains("127.0.0.1")) return;

                Util.Log($"CMD: {user}@{host}: {cmd}");
                (errCode, result) = ShellHelper.RemoteBash(user, host, sshPort, password, cmd);
                if (errCode != 0) return;
            });

            if (errCode != 0)
            {
                Util.Log($"ERR {errCode}: {result}");
                Environment.Exit(1);
            }

            return (errCode, result);
        }

        public static(int, string) GitCloneRepo(List<string> hosts, string repoUrl, string user, string password, int sshPort,
            string commit = "", string branch = "origin/master", string repoRoot = "/home/wanl/signalr_auto_test_framework")
        {
            var errCode = 0;
            var result = "";

            var tasks = new List<Task>();

            hosts.ForEach(host =>
            {
                if (host.Contains("localhost") || host.Contains("127.0.0.1")) return;
                tasks.Add(Task.Run(() =>
                {
                    var errCodeInner = 0;
                    var resultInner = "";
                    var cmdInner = $"rm -rf {repoRoot}  || true; git clone {repoUrl} {repoRoot}; "; //TODO
                    cmdInner += $"cd {repoRoot};";
                    cmdInner += $"git checkout {branch};";
                    if (commit != null && commit != "") cmdInner += $"git reset --hard {commit};";
                    cmdInner += $" cd ~ ;";
                    Util.Log($"CMD: {user}@{host}: {cmdInner}");
                    (errCodeInner, resultInner) = ShellHelper.RemoteBash(user, host, sshPort, password, cmdInner);
                    if (errCodeInner != 0)
                    {
                        errCode = errCodeInner;
                        result = resultInner;
                    }
                }));
            });

            Task.WhenAll(tasks).Wait();

            if (errCode != 0)
            {
                Util.Log($"ERR {errCode}: {result}");
                Environment.Exit(1);
            }

            return (errCode, result);
        }

        public static(int, string) ScpRepo(List<string> hosts, string repoUrl, string user, string password, int sshPort,
            string commit = "", string branch = "origin/master", string repoRoot = "/home/wanl/signalr_auto_test_framework")
        {
            var errCode = 0;
            var result = "";

            var tasks = new List<Task>();

            hosts.ForEach(host =>
            {
                tasks.Add(Task.Run(() =>
                {
                    var errCodeInner = 0;
                    var resultInner = "";

                    if (host.Contains("localhost") || host.Contains("127.0.0.1")) return;

                    // clear old repo
                    var cmdInner = $"rm -rf {repoRoot};"; //TODO
                    Util.Log($"CMD: {user}@{host}: {cmdInner}");
                    (errCodeInner, resultInner) = ShellHelper.RemoteBash(user, host, sshPort, password, cmdInner);

                    if (errCodeInner != 0)
                    {
                        errCode = errCodeInner;
                        result = resultInner;
                    }

                    // scp local repo to remote
                    ScpDirecotryLocalToRemote(user, host, password, repoRoot, repoRoot);

                    // // set node on git
                    // cmdInner = $"cd {repoRoot};";
                    // cmdInner += $"git checkout {branch};";
                    // cmdInner += $"git reset --hard {commit};";
                    // cmdInner += $" cd ~ ;";
                    // Util.Log($"CMD: {user}@{host}: {cmdInner}");
                    // (errCodeInner, resultInner) = ShellHelper.RemoteBash(user, host, sshPort, password, cmdInner);

                    if (errCodeInner != 0)
                    {
                        errCode = errCodeInner;
                        result = resultInner;
                    }
                }));
            });

            Task.WhenAll(tasks).Wait();

            if (errCode != 0)
            {
                Util.Log($"ERR {errCode}: {result}");
                Environment.Exit(1);
            }

            return (errCode, result);
        }
        public static(int, string) StartAppServer(List<string> hosts, string user, string password, int sshPort, List<string> azureSignalrConnectionStrings,
            List<string> logPath, string useLocalSingalR = "false", string appSvrRoot = "/home/wanl/signalr_auto_test_framework")
        {
            var errCode = 0;
            var result = "";
            var cmd = "";

            for (var i = 0; i < hosts.Count; i++)
            {
                cmd = $"cd {appSvrRoot}; " +
                    $"export Azure__SignalR__ConnectionString='{azureSignalrConnectionStrings[i]}'; " +
                    $"export useLocalSignalR={useLocalSingalR}; " +
                    $"dotnet run > {logPath[i]}";
                Util.Log($"{user}@{hosts[i]}: {cmd}");
                (errCode, result) = ShellHelper.RemoteBash(user, hosts[i], sshPort, password, cmd, wait : false);

                if (errCode != 0)
                {
                    Util.Log($"ERR {errCode}: {result}");
                    Environment.Exit(1);
                }
            }

            return (errCode, result);

        }

        public static(int, string) StartRpcSlaves(List<string> slaves, string user, string password, int sshPort, int rpcPort,
            List<string> logPath, string slaveRoot)
        {
            var errCode = 0;
            var result = "";
            var cmd = "";

            for (var i = 0; i < slaves.Count; i++)
            {
                cmd = $"cd {slaveRoot}; dotnet run -- --rpcPort {rpcPort} -d 0.0.0.0 > {logPath[i]}";
                Util.Log($"CMD: {user}@{slaves[i]}: {cmd}");
                (errCode, result) = ShellHelper.RemoteBash(user, slaves[i], sshPort, password, cmd, wait : false);
                if (errCode != 0) break;
            };
            if (errCode != 0)
            {
                Util.Log($"ERR {errCode}: {result}");
                Environment.Exit(1);
            }

            return (errCode, result);

        }

        public static(int, string) StartRpcMaster(string host, List<string> slaves, string user, string password, int sshPort,
            string logPath,
            string serviceType, string transportType, string hubProtocol, string scenario,
            int connection, int concurrentConnection, int duration, int interval, List<string> pipeLine,
            int groupNum, int groupOverlap, string messageSize, string serverUrl, string suffix, string masterRoot, string sendToFixedClient)
        {

            Util.Log($"service type: {serviceType}, transport type: {transportType}, hub protocol: {hubProtocol}, scenario: {scenario}");
            var errCode = 0;
            var result = "";
            var cmd = "";

            (errCode, result) = RemoteBash(user, host, sshPort, password, "cd ~; pwd;");
            var userRoot = result.Substring(0, result.Length - 1);
            Util.Log($"user root: {userRoot}");
            var slaveList = "";

            for (var i = 0; i < slaves.Count; i++)
            {
                slaveList += slaves[i];
                if (i < slaves.Count - 1)
                    slaveList += ";";
            }

            var clear = "false";
            var outputCounterFile = "";

            // todo
            var outputCounterDir = Path.Join(userRoot, $"results/{Environment.GetEnvironmentVariable("result_root")}/{suffix}/");
            outputCounterFile = outputCounterDir + $"counters.txt";

            cmd = $"cd {masterRoot}; ";
            cmd += $"mkdir -p {outputCounterDir} || true;";
            cmd += $"dotnet run -- " +
                $"--rpcPort 5555 " +
                $"--duration {duration} --connections {connection} --interval {interval} --slaves {slaves.Count} --serverUrl '{serverUrl}' --pipeLine '{string.Join(";", pipeLine)}' " +
                $"-v {serviceType} -t {transportType} -p {hubProtocol} -s {scenario} " +
                $" --slaveList '{slaveList}' " +
                $" --retry {0} " +
                $" --clear {clear} " +
                $" --concurrentConnection {concurrentConnection} " +
                $" --groupNum {groupNum} " +
                $" --groupOverlap {groupOverlap} " +
                $"--messageSize {messageSize} " +
                $"--sendToFixedClient true " +
                $" -o '{outputCounterFile}' > {logPath}";

            Util.Log($"CMD: {user}@{host}: {cmd}");
            (errCode, result) = ShellHelper.RemoteBash(user, host, sshPort, password, cmd);

            if (errCode != 0)
            {
                Util.Log($"ERR {errCode}: {result}");
            }

            return (errCode, result);

        }

        public static(int, string) StartSignalrService(List<string> hosts, string user, string password, int sshPort, string serviceDir, List<string> logPath)
        {
            var errCode = 0;
            var result = "";
            var cmd = "";

            for (var i = 0; i < hosts.Count; i++)
            {
                cmd = $"cd {serviceDir}; dotnet run > {logPath[i]}";
                Util.Log($"{user}@{hosts[i]}: {cmd}");
                (errCode, result) = ShellHelper.RemoteBash(user, hosts[i], sshPort, password, cmd, wait : false);

                if (errCode != 0)
                {
                    Util.Log($"ERR {errCode}: {result}");
                    Environment.Exit(1);
                }
            }

            return (errCode, result);

        }
        public static(int, string) CreateSignalrService(ArgsOption argsOption, int unitCount)
        {
            var errCode = 0;
            var result = "";
            var cmd = "";

            var content = AzureBlobReader.ReadBlob("SignalrConfigFileName");
            Console.WriteLine($"content: {content}");
            var config = AzureBlobReader.ParseYaml<SignalrConfig>(content);

            // login to azure
            cmd = $"az login --service-principal --username {config.AppId} --password {config.Password} --tenant {config.Tenant}";
            Util.Log($"CMD: signalr service: az login");
            (errCode, result) = ShellHelper.Bash(cmd, handleRes : true);

            // change subscription
            cmd = $"az account set --subscription {config.Subscription}";
            Util.Log($"CMD: az account set --subscription");
            (errCode, result) = ShellHelper.Bash(cmd, handleRes : true);

            // var groupName = Util.GenResourceGroupName(config.BaseName);
            // var srName = Util.GenSignalRServiceName(config.BaseName);

            var rnd = new Random();
            var SrRndNum = (rnd.Next(10000) * rnd.Next(10000)).ToString();

            var groupName = config.BaseName + "Group";
            var srName = config.BaseName + SrRndNum + "SR";

            cmd = $"  az extension add -n signalr || true";
            Util.Log($"CMD: signalr service: {cmd}");
            (errCode, result) = ShellHelper.Bash(cmd, handleRes : true);

            // create resource group
            cmd = $"  az group create --name {groupName} --location {config.Location}";
            Util.Log($"CMD: signalr service: {cmd}");
            (errCode, result) = ShellHelper.Bash(cmd, handleRes : true);

            //create signalr service
            cmd = $"az signalr create --name {srName} --resource-group {groupName}  --sku {config.Sku} --unit-count {unitCount} --query hostName -o tsv";
            Util.Log($"CMD: signalr service: {cmd}");
            (errCode, result) = ShellHelper.Bash(cmd, handleRes : true);

            var signalrHostName = result;
            Console.WriteLine($"signalrHostName: {signalrHostName}");

            // get access key
            cmd = $"az signalr key list --name {srName} --resource-group {groupName} --query primaryKey -o tsv";
            Util.Log($"CMD: signalr service: {cmd}");
            (errCode, result) = ShellHelper.Bash(cmd, handleRes : true);
            var signalrPrimaryKey = result;
            Console.WriteLine($"signalrPrimaryKey: {signalrPrimaryKey}");

            // combine to connection string
            signalrHostName = signalrHostName.Substring(0, signalrHostName.Length - 1);
            signalrPrimaryKey = signalrPrimaryKey.Substring(0, signalrPrimaryKey.Length - 1);
            var connectionString = $"Endpoint=https://{signalrHostName};AccessKey={signalrPrimaryKey};";
            Console.WriteLine($"connection string: {connectionString}");
            ShellHelper.Bash($"export AzureSignalRConnectionString='{connectionString}'", handleRes : true);
            return (errCode, connectionString);
        }

        public static(int, string) DeleteSignalr(ArgsOption args)
        {
            var errCode = 0;
            var result = "";
            var cmd = "";

            var content = AzureBlobReader.ReadBlob("SignalrConfigFileName");
            var config = AzureBlobReader.ParseYaml<SignalrConfig>(content);

            var groupName = config.BaseName + "Group";

            // login to azure
            cmd = $"az login --service-principal --username {config.AppId} --password {config.Password} --tenant {config.Tenant}";
            Util.Log($"CMD: signalr service: logint azure");
            (errCode, result) = ShellHelper.Bash(cmd, handleRes : true);

            // delete resource group
            cmd = $"az group delete --name {groupName} --yes";
            Util.Log($"CMD: signalr service: {cmd}");
            (errCode, result) = ShellHelper.Bash(cmd, handleRes : true);

            return (errCode, result);
        }

        public static(int, string) RemoveSyslog(string host, string user, string password, int sshPort)
        {
            var errCode = 0;
            var result = "";
            var cmd = "sudo rm -rf /var/log/syslog";
            Util.Log($"{user}@{host}: {cmd}");
            (errCode, result) = ShellHelper.RemoteBash(user, host, sshPort, password, cmd, handleRes : true);
            return (errCode, result);
        }
        public static(int, string) PrepareLogPath(string host, string user, string password, int sshPort,
            string dstDir, string time, string suffix)
        {

            var targetDir = Path.Join(dstDir, time);

            var errCode = 0;
            var result = "";
            var cmd = $"rm -rf ~/logs || true; rm -rf ~/results || true; mkdir -p {targetDir}";

            Util.Log($"{user}@{host}: {cmd}");
            (errCode, result) = ShellHelper.RemoteBash(user, host, sshPort, password, cmd, wait : false);

            if (errCode != 0)
            {
                Util.Log($"ERR {errCode}: {result}");
                Environment.Exit(1);
            }

            result = Path.Join(targetDir, $"log_{suffix}.txt");
            return (errCode, result);

        }

        public static(int, string) CollectStatistics(List<string> hosts, string user, string password, int sshPort, string remote, string local)
        {
            var errCode = 0;
            var result = "";

            hosts.ForEach(host =>
            {
                (errCode, result) = ShellHelper.ScpDirecotryRemoteToLocal(user, host, password, remote, local);
                if (errCode != 0)
                {
                    Util.Log($"ERR {errCode}: {result}");
                    Environment.Exit(1);
                }
            });
            return (errCode, result);
        }

        public static(int, string) TransferServiceRuntimeToVm(List<string> hosts, string user, string password, int sshPort, string srcDirParent, string srcDirName, string dstDir)
        {
            var errCode = 0;
            var result = "";
            var cmd = "";

            (errCode, result) = Bash("pwd", true, true);
            var curDir = result;
            // zip local service repo
            cmd = $"cd {srcDirParent}; zip -r serviceruntime.zip {srcDirName}/*; mv serviceruntime.zip {curDir}";
            (errCode, result) = Bash(cmd, true, true);

            // scp
            foreach (var host in hosts)
            {
                (errCode, result) = ScpFileLocalToRemote(user, host, password, "serviceruntime.zip", "~/");
                if (errCode != 0)
                {
                    Util.Log($"ERR {errCode}: {result}");
                    Environment.Exit(1);
                }

                // install unzip
                cmd = $"sudo apt-get install -y zip; unzip -o -d {dstDir} ~/serviceruntime.zip";
                RemoteBash(user, host, sshPort, password, cmd, handleRes : true);

                // modify appsetting.json
                var appsettings = File.ReadAllText($"{Path.Join(srcDirParent, srcDirName, "src/Microsoft.Azure.SignalR.ServiceRuntime/appsettings.json")}");
                appsettings = appsettings.Replace("localhost", host);
                File.WriteAllText("appsettings.json", appsettings);
                (errCode, result) = ScpFileLocalToRemote(user, host, password, "appsettings.json", Path.Join(dstDir, srcDirName, "src/Microsoft.Azure.SignalR.ServiceRuntime"));

            }

            (errCode, result) = Bash($"cd {curDir}", true, true);

            return (errCode, result);

        }

        public static(int, string) ModifyServiceAppsettings(List<string> hosts, string user, string password, int sshPort, List<string> dnses, string srcDirParent, string srcDirName, string dstDir)
        {
            var errCode = 0;
            var result = "";
            var cmd = "";

            Util.Log($"modify service appsettings");

            if (hosts.Count != dnses.Count)
            {
                throw new Exception();
            }

            for (var i = 0; i < hosts.Count; i++)
            {
                var appsettings = File.ReadAllText($"{Path.Join(srcDirParent, srcDirName, "src/Microsoft.Azure.SignalR.ServiceRuntime/appsettings.json")}");
                appsettings = appsettings.Replace("localhost", dnses[i]);
                File.WriteAllText("appsettings.json", appsettings);
                (errCode, result) = ScpFileLocalToRemote(user, hosts[i], password, "appsettings.json", Path.Join(dstDir, srcDirName, "src/Microsoft.Azure.SignalR.ServiceRuntime"));
            }
            return (errCode, result);
        }

        public static(int, string) CollectMachineStatistics(string host, string user, string password, int sshPort, string path)
        {
            var errCode = 0;
            var result = "";
            var cmd = "top -n 1 -b | head -n 15; exit";
            (errCode, result) = RemoteBash(user, host, sshPort, password, cmd, handleRes : true);

            File.AppendAllText(path, result);

            return (errCode, result);

        }

    }
}