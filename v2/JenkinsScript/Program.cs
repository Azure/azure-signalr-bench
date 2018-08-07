using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
                                vmBuilder.CreateAllVmsInSameVnet();
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
                        var azureSignalrConnectionString = argsOption.AzureSignalrConnectionString;

                        // load private ips
                        var privateIps = configLoader.Load<PrivateIpConfig>(argsOption.PrivateIps);
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

                        var hosts = new List<string>();
                        hosts.Add(privateIps.ServicePrivateIp);
                        hosts.Add(privateIps.AppServerPrivateIp);
                        hosts.AddRange(privateIps.SlavePrivateIp.Split(";"));

                        // prepare log dirs
                        var suffix = "";
                        suffix = GenerateSuffix("service", serviceType, transportType, hubProtocol, scenario, connection, concurrentConnection, groupNum, overlap, isGroupJoinLeave);
                        (errCode, result) = ShellHelper.PrepareLogPath(servicePvtIp, user, password, sshPort, logRoot, resultRoot, suffix);
                        var logPathService = result;

                        suffix = GenerateSuffix("appserver", serviceType, transportType, hubProtocol, scenario, connection, concurrentConnection, groupNum, overlap, isGroupJoinLeave);
                        (errCode, result) = ShellHelper.PrepareLogPath(appserverPvtIp, user, password, sshPort, logRoot, resultRoot, suffix);
                        var logPathAppServer = result;

                        slavesPvtIp.ForEach(ip =>
                        {
                            suffix = GenerateSuffix($"slave{ip}", serviceType, transportType, hubProtocol, scenario, connection, concurrentConnection, groupNum, overlap, isGroupJoinLeave);
                            (errCode, result) = ShellHelper.PrepareLogPath(ip, user, password, sshPort, logRoot, resultRoot, suffix);
                        });
                        var logPathSlave = result;

                        suffix = GenerateSuffix("master", serviceType, transportType, hubProtocol, scenario, connection, concurrentConnection, groupNum, overlap, isGroupJoinLeave);
                        (errCode, result) = ShellHelper.PrepareLogPath(masterPvtIp, user, password, sshPort, logRoot, resultRoot, suffix);
                        var logPathMaster = result;

                        // clone repo to all vms
                        if (!debug) ShellHelper.GitCloneRepo(hosts, remoteRepo, user, password,
                            sshPort, commit: "", branch: branch, repoRoot : localRepoRoot);

                        // kill all dotnet
                        if (!debug) ShellHelper.KillAllDotnetProcess(hosts, remoteRepo, user, password, sshPort, repoRoot : localRepoRoot);

                        // start service
                        if (!debug) (errCode, result) = ShellHelper.StartSignalrService(privateIps.ServicePrivateIp, user, password, sshPort, serviceDir, logPathService);
                        Task.Delay(waitTime).Wait();

                        // start app server
                        ShellHelper.StartAppServer(privateIps.AppServerPrivateIp, user, password, sshPort, azureSignalrConnectionString, logPathAppServer, useLocalSignalR, appSvrRoot);
                        Task.Delay(waitTime).Wait();

                        // start slaves
                        ShellHelper.StartRpcSlaves(privateIps.SlavePrivateIp.Split(";").ToList(), user, password, sshPort, rpcPort, logPathSlave, slaveRoot);
                        Task.Delay(waitTime).Wait();

                        // start master
                        ShellHelper.StartRpcMaster(privateIps.MasterPrivateIp, privateIps.SlavePrivateIp.Split(";").ToList(), user, password, sshPort, logPathMaster, serviceType, transportType, hubProtocol, scenario, connection, concurrentConnection, duration, interval, pipeline, groupNum, overlap, serverUrl, suffix, masterRoot);

                        // collect all logs
                        ShellHelper.CollectStatistics(hosts, user, password, sshPort, "~/logs", Util.MakeSureDirectoryExist("~/signalr-bench-statistics/logs"));

                        // collect results from master
                        ShellHelper.CollectStatistics((new string[] { privateIps.MasterPrivateIp }).ToList(), user, password, sshPort, "~/results", Util.MakeSureDirectoryExist("~/signalr-bench-statistics/results"));

                        break;
                    }

                    // case "debugmaclocal":
                    //     {
                    //     //     var repoRoot = "/Users/albertxavier/workspace/signalr_auto_test_framework";

                    //     //     // create agent & appserver vms
                    //     //     agentConfig.AppServer = "localhost";
                    //     //     agentConfig.Slaves = new List<string>();
                    //     //     agentConfig.Slaves.Add("localhost");

                    //     //     // genrate host list
                    //     //     var hosts = new List<string>();
                    //     //     hosts.Add(agentConfig.AppServer);
                    //     //     agentConfig.Slaves.ForEach(slv => hosts.Add(slv));
                    //     //     hosts.Add(agentConfig.Master);

                    //     //     // TODO: check if ssh success
                    //     //     Task.Delay(20 * 1000).Wait();

                    //     //     var types = jobConfig.ServiceTypeList;
                    //     //     var isSelfHost = true;
                    //     //     if (jobConfig.ServiceTypeList == null || jobConfig.ServiceTypeList.Count == 0)
                    //     //     {
                    //     //         types = jobConfig.SignalrUnit;
                    //     //         isSelfHost = false;
                    //     //     }

                    //     //     int indType = 0;
                    //     //     foreach (var serviceType in types)
                    //     //     {
                    //     //         var unit = 1;
                    //     //         unit = Convert.ToInt32(serviceType.Substring(4));
                    //     //         foreach (var transportType in jobConfig.TransportTypeList)
                    //     //         {
                    //     //             foreach (var hubProtocol in jobConfig.HubProtocolList)
                    //     //             {
                    //     //                 foreach (var scenario in jobConfig.ScenarioList)
                    //     //                 {
                    //     //                     (int connectionBase, int connectionIncreaseStep, int connectionLength) = GetConnectionConfig(scenario, jobConfig, indType);
                    //     //                     (int groupNumBase, int groupNumStep, int groupNumLength) = GetGroupNumConfig(scenario, jobConfig, indType);

                    //     //                     for (var connection = connectionBase; connection < connectionBase + connectionIncreaseStep * jobConfig.ConnectionLength; connection += connectionIncreaseStep)
                    //     //                     {

                    //     //                         for (var groupNum = groupNumBase; groupNum < groupNumBase + groupNumStep * groupNumLength; groupNum += groupNumStep)
                    //     //                         {
                    //     //                             RunJob(serviceType, transportType, hubProtocol, scenario, connection, groupNum, jobConfig, agentConfig, argsOption, hosts, repoRoot, serverUrl: "localhost", useLocalSignalR: "false", waitTime : TimeSpan.FromSeconds(5));
                    //     //                         }
                    //     //                     }
                    //     //                 }
                    //     //             }
                    //     //         }
                    //     //         indType++;
                    //     //     }
                    //     //     break;
                    //     // }
                    // case "All":
                    // default:
                    //     {
                    //         // create agent & appserver vms
                    //         while (true)
                    //         {
                    //             try
                    //             {
                    //                 var createResourceTasks = new List<Task>();
                    //                 createResourceTasks.Add(vmBuilder.CreateAppServerVm());
                    //                 createResourceTasks.Add(vmBuilder.CreateAgentVms());
                    //                 Task.WhenAll(createResourceTasks).Wait();

                    //             }
                    //             catch (Exception ex)
                    //             {
                    //                 Util.Log($"creating VMs Exception: {ex}");
                    //                 Util.Log($"delete all vms");
                    //                 azureManager.DeleteResourceGroup(vmBuilder.GroupName);
                    //                 azureManager.DeleteResourceGroup(vmBuilder.AppSvrGroupName);
                    //                 Util.Log($"going to retry creating vms in 1s");
                    //                 Task.Delay(1000).Wait();
                    //                 continue;
                    //             }
                    //             break;
                    //         }

                    //         agentConfig.AppServer = vmBuilder.AppSvrDomainName();
                    //         agentConfig.Slaves = new List<string>();
                    //         for (var i = 0; i < agentConfig.SlaveVmCount; i++)
                    //         {
                    //             agentConfig.Slaves.Add(vmBuilder.SlaveDomainName(i));
                    //         }

                    //         // genrate host list
                    //         var hosts = new List<string>();
                    //         hosts.Add(agentConfig.AppServer);
                    //         agentConfig.Slaves.ForEach(slv => hosts.Add(slv));
                    //         hosts.Add(agentConfig.Master);

                    //         // TODO: check if ssh success
                    //         Task.Delay(20 * 1000).Wait();

                    //         (errCode, result) = ShellHelper.KillAllDotnetProcess(hosts, agentConfig, argsOption);
                    //         (errCode, result) = ShellHelper.GitCloneRepo(hosts, agentConfig, argsOption.Commit, argsOption.Branch);

                    //         var types = jobConfig.ServiceTypeList;
                    //         var isSelfHost = true;
                    //         if (jobConfig.ServiceTypeList == null || jobConfig.ServiceTypeList.Count == 0)
                    //         {
                    //             types = jobConfig.SignalrUnit;
                    //             isSelfHost = false;
                    //         }

                    //         int indType = 0;
                    //         foreach (var serviceType in types)
                    //         {
                    //             var unit = 1;
                    //             unit = Convert.ToInt32(serviceType.Substring(4));

                    //             // create signalr service
                    //             if (argsOption.AzureSignalrConnectionString == null || argsOption.AzureSignalrConnectionString == "")
                    //             {
                    //                 while (true)
                    //                 {
                    //                     try
                    //                     {
                    //                         var createSignalrR = Task.Run(() =>
                    //                         {
                    //                             (errCode, argsOption.AzureSignalrConnectionString) = ShellHelper.CreateSignalrService(argsOption, unit);
                    //                         });
                    //                         Task.WhenAll(createSignalrR).Wait();
                    //                     }
                    //                     catch (Exception ex)
                    //                     {
                    //                         Util.Log($"Creating SignalR Exception: {ex}");
                    //                         Util.Log($"deleting all signalr services");
                    //                         (errCode, result) = ShellHelper.DeleteSignalr(argsOption); // TODO what if delete fail
                    //                         Util.Log($"going to retry creating signalr service in 1s");
                    //                         Task.Delay(1000).Wait();
                    //                         continue;
                    //                     }
                    //                     break;
                    //                 }
                    //             }

                    //             foreach (var transportType in jobConfig.TransportTypeList)
                    //             {
                    //                 foreach (var hubProtocol in jobConfig.HubProtocolList)
                    //                 {
                    //                     foreach (var scenario in jobConfig.ScenarioList)
                    //                     {
                    //                         (int connectionBase, int connectionIncreaseStep, int connectionLength) = GetConnectionConfig(scenario, jobConfig, indType);
                    //                         (int groupNumBase, int groupNumStep, int groupNumLength) = GetGroupNumConfig(scenario, jobConfig, indType);

                    //                         for (var connection = connectionBase; connection < connectionBase + connectionIncreaseStep * connectionLength; connection += connectionIncreaseStep)
                    //                         {
                    //                             for (var groupNum = groupNumBase; groupNum < groupNumBase + groupNumStep * groupNumLength; groupNum += groupNumStep)
                    //                             {
                    //                                 RunJob(serviceType, transportType, hubProtocol, scenario, connection, groupNum, jobConfig, agentConfig, argsOption, hosts, repoRoot: "~/signalr_auto_test_framework", serverUrl : vmBuilder.AppSvrDomainName(), argsOption.UseLocalSignalR, waitTime : TimeSpan.FromSeconds(20));
                    //                             }
                    //                         }
                    //                     }
                    //                 }
                    //             }
                    //             indType++;
                    //             if (argsOption.UseLocalSignalR == "true" ||
                    //                 argsOption.AzureSignalrConnectionString == null ||
                    //                 argsOption.AzureSignalrConnectionString == "")
                    //                 (errCode, result) = ShellHelper.DeleteSignalr(argsOption);

                    //             //(errCode, result) = ShellHelper.DeleteSignalr(argsOption);
                    //         }
                    //         //(errCode, result) = ShellHelper.GenerateAllReports(hosts, agentConfig);

                    //         //azureManager.DeleteResourceGroup(vmBuilder.GroupName);
                    //         //azureManager.DeleteResourceGroup(vmBuilder.AppSvrGroupName);
                    //         break;
                    //     }
            }

        }

        // static private(int, int, int) GetConnectionConfig(string scenario, JobConfig jobConfig, int indType)
        // {
        //     var propName = scenario.First().ToString().ToUpper() + scenario.Substring(1);
        //     var connectionBase = 0;
        //     var connectionIncreaseStep = 1;
        //     var connectionLength = 1;
        //     if (propName.ToLower().Contains("echo") || propName.ToLower().Contains("broadcast"))
        //     {
        //         connectionBase = (jobConfig.ConnectionBase.GetType().GetProperty(propName).GetValue(jobConfig.ConnectionBase) as List<int>) [indType];
        //         connectionIncreaseStep = (jobConfig.ConnectionIncreaseStep.GetType().GetProperty(propName).GetValue(jobConfig.ConnectionIncreaseStep) as List<int>) [indType];
        //         connectionLength = jobConfig.ConnectionLength;
        //     }
        //     else if (propName.ToLower().Contains("group"))
        //     {
        //         connectionBase = jobConfig.Group.GroupConnectionBase[indType];
        //         connectionIncreaseStep = jobConfig.Group.GroupConnectionStep[indType];
        //         connectionLength = jobConfig.Group.GroupConnectionLength;
        //     }
        //     else
        //     {
        //         connectionBase = jobConfig.Mix.MixEchoConnection + jobConfig.Mix.MixBroadcastConnection + jobConfig.Mix.MixGroupConnection;
        //         connectionIncreaseStep = 1;
        //         connectionLength = 1;
        //     }

        //     return (connectionBase, connectionIncreaseStep, connectionLength);
        // }

        // static private(int, int, int) GetGroupNumConfig(string scenario, JobConfig jobConfig, int indType)
        // {
        //     var groupNumBase = 0;
        //     var groupNumStep = 1;
        //     var groupNumLength = 1;
        //     if (scenario.ToLower().Contains("group"))
        //     {
        //         groupNumBase = jobConfig.Group.GroupNumBase[indType];
        //         groupNumStep = jobConfig.Group.GroupNumStep[indType];
        //         groupNumLength = jobConfig.Group.GroupNumLength;
        //     }
        //     return (groupNumBase, groupNumStep, groupNumLength);
        // }

        // static private void RunJob(string serviceType, string transportType, string hubProtocol, string scenario, int connection, int groupNum,
        //     JobConfig jobConfig, AgentConfig agentConfig, ArgsOption argsOption,
        //     List<string> hosts, string repoRoot, string serverUrl, string useLocalSignalR, TimeSpan waitTime)
        // {
        //     var errCode = 0;
        //     var result = "";
        //     Util.Log($"current connection: {connection}, duration: {jobConfig.Duration}, interval: {jobConfig.Interval}, transport type: {transportType}, protocol: {hubProtocol}, scenario: {scenario}");
        //     (errCode, result) = ShellHelper.KillAllDotnetProcess(hosts, agentConfig, argsOption, repoRoot);
        //     Task.Delay(waitTime).Wait();
        //     // (errCode, result) = ShellHelper.StartSelfhostSignalrService(ServiceUrl, ServicePassword, ServicePort);
        //     // Task.Delay(waitTime).Wait();
        //     (errCode, result) = ShellHelper.StartAppServer(hosts, agentConfig, argsOption.AzureSignalrConnectionString, serviceType, transportType, hubProtocol, scenario, connection, useLocalSignalR, repoRoot);
        //     Task.Delay(waitTime).Wait();
        //     (errCode, result) = ShellHelper.StartRpcSlaves(agentConfig, argsOption, serviceType, transportType, hubProtocol, scenario, connection, repoRoot);
        //     Task.Delay(waitTime).Wait();
        //     (errCode, result) = ShellHelper.StartRpcMaster(agentConfig, argsOption,
        //         serviceType, transportType, hubProtocol, scenario, connection, jobConfig.Duration,
        //         jobConfig.Interval, string.Join(";", jobConfig.Pipeline),
        //         jobConfig.Mix.MixEchoConnection, jobConfig.Mix.MixBroadcastConnection, jobConfig.Mix.MixGroupConnection, jobConfig.Mix.MixGroupName,
        //         groupNum,
        //         serverUrl, repoRoot);
        // }

        private static string GenerateSuffix(string agent, string serviceType, string transportType, string hubProtocol,
            string scenario, int connections, int concurrentConnection, int groupNum = 0, int overlap = 0, bool isGroupJoinLeave = false)
        {
            return $"{agent}_{serviceType}_{transportType}_{hubProtocol}_{scenario}_{connections}_{concurrentConnection}_{groupNum}_{overlap}_{(isGroupJoinLeave ? "true" : "false")}";
        }

        private static void SavePid(string pidFile)
        {
            var pid = Process.GetCurrentProcess().Id;
            if (pidFile != null)
            {
                File.AppendAllText(pidFile, $"{pid}");
            }
        }
    }
}