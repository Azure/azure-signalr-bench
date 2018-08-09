using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using CommandLine;

namespace JenkinsScript
{
    class Program
    {
        static void Main(string[] args)
        {
            bool invalidInput = false;
            // read options
            var argsOption = new ArgsOption();
            _ = Parser.Default.ParseArguments<ArgsOption>(args)
                .WithParsed(options => argsOption = options)
                .WithNotParsed(error =>
                {
                    invalidInput = true;
                    Util.Log($"error occurs: {error}");
                });
            if (invalidInput)
            {
                return;
            }

            SavePid(argsOption.PidFile);

            var resourceGroupName = "";
            var signalrServiceName = "";

            var errCode = 0;
            var result = "";

            switch (argsOption.Step)
            {
                // case "CreateSignalr":
                //     (errCode, result) = ShellHelper.CreateSignalrService(argsOption, 10);
                //     break;
                // case "DeleteSignalr":
                //     (errCode, result) = ShellHelper.DeleteSignalr(argsOption);
                //     break;
                // case "CreateAllAgentVMs":
                //     vmBuilder.CreateAgentVmsCore();
                //     break;
                // case "DeleteAllAgentVMs":
                //     azureManager.DeleteResourceGroup(vmBuilder.GroupName);
                //     break;
                // case "CreateAppServerVm":
                //     vmBuilder.CreateAppServerVmCore();
                //     break;
                case "CreateDogfoodSignalr":
                    if (argsOption.ExtensionScriptDir == null)
                    {
                        Util.Log("extension scripts directory is not specified, so this function does not work");
                    }
                    else
                    {
                        var destFile = System.IO.Path.Combine(argsOption.ExtensionScriptDir, "utils.sh");
                        if (argsOption.UtilsFilePath != null)
                        {
                            System.IO.File.Copy(argsOption.UtilsFilePath, destFile, true);
                        }
                        var postfix = Util.GenRandPrefix();
                        resourceGroupName = Util.GenResourceGroupName(postfix);
                        signalrServiceName = Util.GenSignalRServiceName(postfix);
                        var connectionString = DogfoodSignalROps.CreateDogfoodSignalRService(argsOption.ExtensionScriptDir, argsOption.Location, resourceGroupName, signalrServiceName, "Basic_DS2", argsOption.SignalRUnit);
                        if (connectionString != null)
                        {
                            Util.Log($"Connection string is {connectionString} under resource group {resourceGroupName}");
                        }
                    }
                    break;
                case "DeleteDogfoodSignalr":
                    if (argsOption.ExtensionScriptDir == null)
                    {
                        Util.Log("extension scripts directory is not specified, so this function does not work");
                    }
                    else
                    {
                        if (argsOption.SignalRService == null || argsOption.ResourceGroup == null)
                        {
                            Util.Log("Please specify SignalR Service name and Resource Group you want to delete");
                        }
                        else
                        {
                            var destFile = System.IO.Path.Combine(argsOption.ExtensionScriptDir, "utils.sh");
                            if (argsOption.UtilsFilePath != null)
                            {
                                System.IO.File.Copy(argsOption.UtilsFilePath, destFile, true);
                            }
                            DogfoodSignalROps.DeleteDogfoodSignalRService(argsOption.ExtensionScriptDir, argsOption.ResourceGroup, argsOption.SignalRService);
                        }
                    }
                    break;
                case "RegisterDogfoodCloud":
                    if (argsOption.ExtensionScriptDir == null)
                    {
                        Util.Log("extension scripts directory is not specified, so this function does not work");
                    }
                    else
                    {
                        DogfoodSignalROps.RegisterDogfoodCloud(argsOption.ExtensionScriptDir);
                    }
                    break;
                case "UnregisterDogfoodCloud":
                    if (argsOption.ExtensionScriptDir == null)
                    {
                        Util.Log("extension scripts directory is not specified, so this function does not work");
                    }
                    else
                    {
                        DogfoodSignalROps.UnregisterDogfoodCloud(argsOption.ExtensionScriptDir);
                    }
                    break;
                case "CreateBenchServer":
                    {
                        // parse agent config file
                        AgentConfig agentConfig = new AgentConfig();
                        var configLoader = new ConfigLoader();

                        if (argsOption.AgentConfigFile != null)
                            agentConfig = configLoader.Load<AgentConfig>(argsOption.AgentConfigFile);

                        AzureManager azureManager = null;
                        BenchmarkVmBuilder vmBuilder = null;
                        if (argsOption.ExtensionScriptDir == null)
                        {
                            azureManager = new AzureManager();
                            vmBuilder = new BenchmarkVmBuilder(agentConfig);
                        }

                        while (true)
                        {
                            try
                            {
                                vmBuilder.CreateBenchServer();
                            }
                            catch (Exception ex)
                            {
                                Util.Log($"creating VMs Exception: {ex}");
                                Util.Log($"delete all vms");
                                azureManager.DeleteResourceGroup(vmBuilder.GroupName);
                                azureManager.DeleteResourceGroup(vmBuilder.AppSvrGroupName);
                                Util.Log($"going to retry creating vms in 1s");
                                Task.Delay(1000).Wait();
                                continue;
                            }
                            break;
                        }

                        break;
                    }
                case "CreateAllVmsInSameVnet":
                    {
                        // parse agent config file
                        AgentConfig agentConfig = new AgentConfig();
                        var configLoader = new ConfigLoader();

                        if (argsOption.AgentConfigFile != null)
                            agentConfig = configLoader.Load<AgentConfig>(argsOption.AgentConfigFile);

                        AzureManager azureManager = null;
                        BenchmarkVmBuilder vmBuilder = null;
                        if (argsOption.ExtensionScriptDir == null)
                        {
                            azureManager = new AzureManager();
                            vmBuilder = new BenchmarkVmBuilder(agentConfig);
                        }

                        while (true)
                        {
                            try
                            {
                                vmBuilder.CreateAllVmsInSameVnet(argsOption.VnetGroupName, argsOption.VnetName, argsOption.SubnetName, agentConfig.AppSvrVmCount, agentConfig.SvcVmCount);
                            }
                            catch (Exception ex)
                            {
                                Util.Log($"creating VMs Exception: {ex}");
                                Util.Log($"delete all vms");
                                azureManager.DeleteResourceGroup(vmBuilder.GroupName);
                                azureManager.DeleteResourceGroup(vmBuilder.AppSvrGroupName);
                                Util.Log($"going to retry creating vms in 1s");
                                Task.Delay(1000).Wait();
                                continue;
                            }
                            break;
                        }
                        break;
                    }
                case "TransferServiceRuntimeToVm":
                    {
                        // parse agent config file
                        AgentConfig ac = new AgentConfig();
                        var configLoader = new ConfigLoader();

                        var privateIps = configLoader.Load<PrivateIpConfig>(argsOption.PrivateIps);

                        if (argsOption.AgentConfigFile != null)
                            ac = configLoader.Load<AgentConfig>(argsOption.AgentConfigFile);

                        ShellHelper.TransferServiceRuntimeToVm(privateIps.ServicePrivateIp.Split(";").ToList(), ac.User, ac.Password, ac.SshPort, $"/home/{ac.User}", "OSSServices-SignalR-Service", $"/home/{ac.User}");
                        break;
                    }
                case "AllInSameVnet":
                    {
                        // parse agent config file
                        AgentConfig agentConfig = new AgentConfig();
                        var configLoader = new ConfigLoader();

                        if (argsOption.AgentConfigFile != null)
                            agentConfig = configLoader.Load<AgentConfig>(argsOption.AgentConfigFile);

                        var debug = argsOption.Debug;

                        // app server
                        var useLocalSignalR = debug && argsOption.AzureSignalrConnectionString == "" ? "true" : "false";
                        var azureSignalrConnectionStrings = argsOption.AzureSignalrConnectionString.Split("^").ToList();

                        // load private ips
                        var privateIps = configLoader.Load<PrivateIpConfig>(argsOption.PrivateIps);
                        // load public ips
                        var publicIps = configLoader.Load<PublicIpConfig>(argsOption.PublicIps);
                        // load job config v2
                        var jobConfigV2 = configLoader.Load<JobConfigV2>(argsOption.JobConfigFileV2);

                        var servicePvtIp = privateIps.ServicePrivateIp;
                        var appserverPvtIp = privateIps.AppServerPrivateIp;
                        var slavesPvtIp = privateIps.SlavePrivateIp.Split(";").ToList();
                        var masterPvtIp = privateIps.MasterPrivateIp;

                        var serviceDir = "~/OSSServices-SignalR-Service/src/Microsoft.Azure.SignalR.ServiceRuntime";

                        // agent config
                        var user = agentConfig.User;
                        var password = agentConfig.Password;
                        var sshPort = agentConfig.SshPort;
                        var rpcPort = agentConfig.RpcPort;
                        var remoteRepo = agentConfig.Repo;
                        var localRepoRoot = debug ? "~/workspace/azure-signalr-bench/" : "~/signalr-bench";
                        var appSvrRoot = Path.Join(localRepoRoot, "v2/AppServer/");
                        var masterRoot = Path.Join(localRepoRoot, "v2/Rpc/Bench.Client/");
                        var slaveRoot = Path.Join(localRepoRoot, "v2/Rpc/Bench.Server/");
                        var logRoot = "~/logs";
                        var resultRoot = Environment.GetEnvironmentVariable("result_root");
                        var waitTime = TimeSpan.FromSeconds(5);
                        var branch = argsOption.Branch;
                        var serviceVmCnt = agentConfig.SvcVmCount;
                        var appserverVmCount = agentConfig.AppSvrVmCount;

                        // benchmark config
                        var serviceType = jobConfigV2.ServiceType;
                        var transportType = jobConfigV2.TransportType;
                        var hubProtocol = jobConfigV2.HubProtocol;
                        var scenario = jobConfigV2.Scenario;
                        var connection = jobConfigV2.Connection;
                        var concurrentConnection = jobConfigV2.ConcurrentConnection;
                        var duration = jobConfigV2.Duration;
                        var interval = jobConfigV2.Interval;
                        var groupNum = jobConfigV2.GroupNum;
                        var overlap = jobConfigV2.Overlap;
                        var isGroupJoinLeave = jobConfigV2.IsGroupJoinLeave;
                        var pipeline = jobConfigV2.Pipeline;
                        var serverUrl = jobConfigV2.ServerUrl;
                        var messageSize = jobConfigV2.MessageSize;

                        var hosts = new List<string>();
                        hosts.AddRange(privateIps.ServicePrivateIp.Split(";").ToList());
                        hosts.AddRange(privateIps.AppServerPrivateIp.Split(";").ToList());
                        hosts.Add(privateIps.MasterPrivateIp);
                        hosts.AddRange(privateIps.SlavePrivateIp.Split(";").ToList());

                        // prepare log dirs
                        var suffix = "";
                        var logPathService = new List<string>();
                        foreach (var ip in privateIps.ServicePrivateIp.Split(";").ToList())
                        {
                            suffix = GenerateSuffix($"service{ip}", serviceType, transportType, hubProtocol, scenario, connection, concurrentConnection, groupNum, overlap, isGroupJoinLeave, messageSize);
                            (errCode, result) = ShellHelper.PrepareLogPath(ip, user, password, sshPort, logRoot, resultRoot, suffix);
                            logPathService.Add(result);
                        }

                        var logPathAppServer = new List<string>();
                        foreach (var ip in privateIps.AppServerPrivateIp.Split(";").ToList())
                        {
                            suffix = GenerateSuffix($"appserver{ip}", serviceType, transportType, hubProtocol, scenario, connection, concurrentConnection, groupNum, overlap, isGroupJoinLeave, messageSize);
                            (errCode, result) = ShellHelper.PrepareLogPath(ip, user, password, sshPort, logRoot, resultRoot, suffix);
                            logPathAppServer.Add(result);
                        }

                        var logPathSlave = new List<string>();
                        slavesPvtIp.ForEach(ip =>
                        {
                            suffix = GenerateSuffix($"slave{ip}", serviceType, transportType, hubProtocol, scenario, connection, concurrentConnection, groupNum, overlap, isGroupJoinLeave, messageSize);
                            (errCode, result) = ShellHelper.PrepareLogPath(ip, user, password, sshPort, logRoot, resultRoot, suffix);
                            logPathSlave.Add(result);
                        });

                        suffix = GenerateSuffix("master", serviceType, transportType, hubProtocol, scenario, connection, concurrentConnection, groupNum, overlap, isGroupJoinLeave, messageSize);
                        (errCode, result) = ShellHelper.PrepareLogPath(masterPvtIp, user, password, sshPort, logRoot, resultRoot, suffix);
                        var logPathMaster = result;

                        // clone repo to all vms
                        if (!debug) ShellHelper.GitCloneRepo(hosts, remoteRepo, user, password,
                            sshPort, commit: "", branch : branch, repoRoot : localRepoRoot);

                        // kill all dotnet
                        if (!debug) ShellHelper.KillAllDotnetProcess(hosts, remoteRepo, user, password, sshPort, repoRoot : localRepoRoot);
                        Task.Delay(waitTime).Wait();
                        Task.Delay(waitTime).Wait();

                        // start service
                        if (!debug)
                        {
                            privateIps.ServicePrivateIp.Split(";").ToList().ForEach(host => StartCollectMachineStatisticsTimer(host, user, password, sshPort, Util.MakeSureDirectoryExist($"/home/{user}/signalr-bench-statistics/machine/{resultRoot}/") + $"service{host}.txt", TimeSpan.FromSeconds(1)));
                            ShellHelper.ModifyServiceAppsettings(privateIps.ServicePrivateIp.Split(";").ToList(), user, password, sshPort, publicIps.ServicePublicIp.Split(";").ToList(), $"/home/{user}", "OSSServices-SignalR-Service", $"/home/{user}");
                            (errCode, result) = ShellHelper.StartSignalrService(privateIps.ServicePrivateIp.Split(";").ToList(), user, password, sshPort, serviceDir, logPathService);
                        }
                        Task.Delay(waitTime).Wait();

                        // start app server
                        privateIps.AppServerPrivateIp.Split(";").ToList().ForEach(host => StartCollectMachineStatisticsTimer(host, user, password, sshPort, Util.MakeSureDirectoryExist($"/home/{user}/signalr-bench-statistics/machine/{resultRoot}/") + $"appserver{host}.txt", TimeSpan.FromSeconds(1)));
                        ShellHelper.StartAppServer(privateIps.AppServerPrivateIp.Split(";").ToList(), user, password, sshPort, azureSignalrConnectionStrings, logPathAppServer, useLocalSignalR, appSvrRoot);
                        Task.Delay(waitTime).Wait();

                        // start slaves
                        privateIps.SlavePrivateIp.Split(";").ToList().ForEach(host => StartCollectMachineStatisticsTimer(host, user, password, sshPort, Util.MakeSureDirectoryExist($"/home/{user}/signalr-bench-statistics/machine/{resultRoot}/") + $"slave{host}.txt", TimeSpan.FromSeconds(1)));
                        ShellHelper.StartRpcSlaves(privateIps.SlavePrivateIp.Split(";").ToList(), user, password, sshPort, rpcPort, logPathSlave, slaveRoot);
                        Task.Delay(waitTime).Wait();

                        // start master
                        privateIps.MasterPrivateIp.Split(";").ToList().ForEach(host => StartCollectMachineStatisticsTimer(host, user, password, sshPort, Util.MakeSureDirectoryExist($"/home/{user}/signalr-bench-statistics/machine/{resultRoot}/") + $"master{host}.txt", TimeSpan.FromSeconds(1)));
                        ShellHelper.StartRpcMaster(privateIps.MasterPrivateIp, privateIps.SlavePrivateIp.Split(";").ToList(), user, password, sshPort, logPathMaster, serviceType, transportType, hubProtocol, scenario, connection, concurrentConnection, duration, interval, pipeline, groupNum, overlap, messageSize, serverUrl, suffix, masterRoot);

                        // collect all logs
                        ShellHelper.CollectStatistics(hosts, user, password, sshPort, $"/home/{user}/logs", Util.MakeSureDirectoryExist($"/home/{user}/signalr-bench-statistics/{resultRoot}/"));

                        // collect results from master
                        ShellHelper.CollectStatistics((new string[] { privateIps.MasterPrivateIp }).ToList(), user, password, sshPort, $"/home/{user}/results/", Util.MakeSureDirectoryExist($"/home/{user}/signalr-bench-statistics/{resultRoot}/"));

                        break;
                    }
            }

        }

        private static string GenerateSuffix(string agent, string serviceType, string transportType, string hubProtocol,
            string scenario, int connections, int concurrentConnection, int groupNum = 0, int overlap = 0, bool isGroupJoinLeave = false, string messageSize = "0")
        {
            return $"{agent}_{serviceType}_{transportType}_{hubProtocol}_{scenario}_{connections}_{concurrentConnection}_{groupNum}_{overlap}_{(isGroupJoinLeave ? "true" : "false")}_{messageSize}";
        }

        private static void SavePid(string pidFile)
        {
            var pid = Process.GetCurrentProcess().Id;
            if (pidFile != null)
            {
                File.AppendAllText(pidFile, $"{pid}");
            }
        }

        private static void StartCollectMachineStatisticsTimer(string host, string user, string password, int sshPort, string path, TimeSpan interval)
        {
            var timer = new Timer(interval.TotalMilliseconds);
            timer.AutoReset = true;
            timer.Elapsed += async(sender, e) =>
            {
                ShellHelper.CollectMachineStatistics(host, user, password, sshPort, path);
            };
            timer.Start();

        }
    }
}