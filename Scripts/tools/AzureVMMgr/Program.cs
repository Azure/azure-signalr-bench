using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using CommandLine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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

            if (!string.IsNullOrEmpty(argsOption.PidFile))
            {
                SavePid(argsOption.PidFile);
            }

            var errCode = 0;
            var result = "";

            switch (argsOption.Step)
            {
                case "DeleteResourceGroupByConfig":
                    {
                        AgentConfig agentConfig = new AgentConfig();
                        var configLoader = new ConfigLoader();
                        agentConfig = configLoader.Load<AgentConfig>(argsOption.AgentConfigFile);
                        var azureManager = new AzureManager(argsOption.ServicePrincipal);
                        var vmBuilder = new BenchmarkVmBuilder(agentConfig, argsOption.ServicePrincipal, argsOption.DisableRandomSuffix);
                        azureManager.DeleteResourceGroup(vmBuilder.GroupName);
                        break;
                    }
                case "DeleteResourceGroup":
                    {
                        var azureManager = new AzureManager(argsOption.ServicePrincipal);
                        azureManager.DeleteResourceGroup(argsOption.ResourceGroup);
                        break;
                    }
                case "UpdateServerUrl":
                    {
                        var configLoader = new ConfigLoader();
                        var publicIps = configLoader.Load<PublicIpConfig>(argsOption.PublicIps);
                        var serverUrl = publicIps.AppServerPublicIp.Split(";").ToList().Select(ip => "http://" + ip + ":5050/signalrbench").ToList();
                        var serverUrlStr = String.Join(";", serverUrl);

                        var jobConfigV2 = configLoader.Load<JobConfigV2>(argsOption.JobConfigFileV2);
                        jobConfigV2.ServerUrl = serverUrlStr;
                        var serializer = new SerializerBuilder().WithNamingConvention(new CamelCaseNamingConvention()).Build();
                        var yaml = serializer.Serialize(jobConfigV2);
                        File.WriteAllText(argsOption.JobConfigFileV2, yaml);
                        break;
                    }
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
                            azureManager = new AzureManager(argsOption.ServicePrincipal);
                            vmBuilder = new BenchmarkVmBuilder(agentConfig, argsOption.ServicePrincipal, argsOption.DisableRandomSuffix);
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
                            azureManager = new AzureManager(argsOption.ServicePrincipal);
                            vmBuilder = new BenchmarkVmBuilder(agentConfig, argsOption.ServicePrincipal, argsOption.DisableRandomSuffix);
                        }
                        var i = 0;
                        var retryMax = 5;
                        while (i < retryMax)
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
                                Util.Log($"going to retry creating vms in 1s");
                                Task.Delay(1000).Wait();
                                i++;
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
                        var connectionString = argsOption.ConnectionString;

                        // load private ips
                        var privateIps = configLoader.Load<PrivateIpConfig>(argsOption.PrivateIps);
                        // load public ips
                        var publicIps = configLoader.Load<PublicIpConfig>(argsOption.PublicIps);
                        // load job config v2
                        var jobConfigV2 = configLoader.Load<JobConfigV2>(argsOption.JobConfigFileV2);

                        // IPs
                        var agentsPvtIp = privateIps.AgentPrivateIp.Split(";").ToList();
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
                        var agentRoot = Path.Join(localRepoRoot, "v2/Rpc/Bench.Server/");
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
                        var combineFactor = jobConfigV2.CombineFactor;
                        var enableGroupJoinLeave = jobConfigV2.EnableGroupJoinLeave;
                        var pipeline = jobConfigV2.Pipeline;
                        var serverUrl = jobConfigV2.ServerUrl;
                        var neverStopAppServer = bool.Parse(argsOption.NeverStopAppServer);
                        var messageSize = jobConfigV2.MessageSize;
                        var sendToFixedClient = argsOption.SendToFixedClient;
                        var statisticsSuffix = argsOption.StatisticsSuffix;
                        var appServerInUse = argsOption.AppServerCountInUse;
                        var appServerList = privateIps.AppServerPrivateIp.Split(";").ToList();
                        var appServerInUseList = appServerList.Take(appServerInUse < appServerList.Count() ? appServerInUse : appServerList.Count()).ToList();
                        var statisticFolder = $"/home/{user}/signalr-bench-statistics-{statisticsSuffix}/machine/{resultRoot}/";
                        var logFolder = $"/home/{user}/signalr-bench-statistics-{statisticsSuffix}/logs/";
                        var resultFolder = $"/home/{user}/signalr-bench-statistics-{statisticsSuffix}/results/";
                        var statisticCustomizedFolder = Environment.GetEnvironmentVariable("env_statistic_folder");
                        var resultCustomizedFolder = Environment.GetEnvironmentVariable("env_result_folder");

                        // prepare result directory for regular test
                        var collector = new StatisticsCollector(argsOption.Parent, argsOption.Root, argsOption.Scenario);

                        if (argsOption.Regular)
                        {
                            collector.PrepareDirectory();
                            statisticFolder = collector.MachineDirPath;
                            logFolder = collector.LogDirPath;
                            resultFolder = collector.ResultDirPath;
                        }

                        if (!string.IsNullOrEmpty(statisticCustomizedFolder))
                        {
                            statisticFolder = $"{statisticCustomizedFolder}";
                            logFolder = statisticFolder;
                        }
                        if (!string.IsNullOrEmpty(resultCustomizedFolder))
                        {
                            resultFolder = resultCustomizedFolder;
                        }

                        var hosts = new List<string>();
                        if (privateIps.ServicePrivateIp != null && privateIps.ServicePrivateIp.Length > 0)
                            hosts.AddRange(privateIps.ServicePrivateIp.Split(";").ToList());
                        if (!neverStopAppServer)
                        {
                            hosts.AddRange(appServerInUseList);
                        }
                        else
                        {
                            Util.Log("Never stop app server is enabled");
                        }

                        hosts.Add(privateIps.MasterPrivateIp);
                        hosts.AddRange(privateIps.AgentPrivateIp.Split(";").ToList());

                        // prepare log dirs
                        var suffix = "";
                        var logPathService = new List<string>();
                        if (privateIps.ServicePrivateIp != null && privateIps.ServicePrivateIp.Length > 0)
                        {
                            foreach (var ip in privateIps.ServicePrivateIp.Split(";").ToList())
                            {
                                suffix = GenerateSuffix($"service{ip}");
                                (errCode, result) = ShellHelper.PrepareLogPath(ip, user, password, sshPort, logRoot, resultRoot, suffix);
                                logPathService.Add(result);
                            }
                        }

                        var logPathAppServer = new List<string>();
                        for (var m = 0; m < appServerInUse && m < appServerList.Count(); m++)
                        {
                            var ip = appServerList[m];
                            if (!neverStopAppServer)
                            {
                                suffix = GenerateSuffix($"appserver{ip}");
                                (errCode, result) = ShellHelper.PrepareLogPath(ip, user, password, sshPort, logRoot, resultRoot, suffix);
                                logPathAppServer.Add(result);
                            }
                            else
                            {
                                // set a fixed output log folder
                                (errCode, result) = ShellHelper.PrepareLogPath(ip, user, password, sshPort, logRoot, "", $"appserver{ip}", false);
                                logPathAppServer.Add(result);
                            }
                        }

                        var logPathAgent = new List<string>();
                        agentsPvtIp.ForEach(ip =>
                        {
                            suffix = GenerateSuffix($"agent{ip}");
                            (errCode, result) = ShellHelper.PrepareLogPath(ip, user, password, sshPort, logRoot, resultRoot, suffix);
                            logPathAgent.Add(result);
                        });

                        suffix = "master";
                        var masterSuffix = suffix;
                        (errCode, result) = ShellHelper.PrepareLogPath(masterPvtIp, user, password, sshPort, logRoot, resultRoot, suffix);
                        var logPathMaster = result;

                        // clone repo to all vms
                        if (!debug) ShellHelper.GitCloneRepo(hosts, remoteRepo, user, password,
                                        sshPort, commit: "", branch: branch, repoRoot: localRepoRoot, false);
                        // kill all dotnet
                        if (!debug) ShellHelper.KillAllDotnetProcess(hosts, remoteRepo, user, password, sshPort, repoRoot : localRepoRoot);

                        // specially handle app servers
                        if (neverStopAppServer)
                        {
                            ShellHelper.GitCloneRepo(appServerInUseList, remoteRepo, user, password,
                                        sshPort, commit: "", branch: branch, repoRoot: localRepoRoot, false);
                        }
                        Task.Delay(waitTime).Wait();
                        Task.Delay(waitTime).Wait();

                        // start service
                        if (!debug && privateIps.ServicePrivateIp != null && privateIps.ServicePrivateIp.Length > 0)
                        {

                            privateIps.ServicePrivateIp.Split(";").ToList().ForEach(host => StartCollectMachineStatisticsTimer(host, user, password, sshPort, Path.Combine(Util.MakeSureDirectoryExist(statisticFolder), $"service{host}.txt"), TimeSpan.FromSeconds(1)));
                            // if () StartCollectMachineStatisticsTimer()
                            ShellHelper.ModifyServiceAppsettings(privateIps.ServicePrivateIp.Split(";").ToList(), user, password, sshPort, publicIps.ServicePublicIp.Split(";").ToList(), $"/home/{user}", "OSSServices-SignalR-Service", $"/home/{user}");
                            (errCode, result) = ShellHelper.StartSignalrService(privateIps.ServicePrivateIp.Split(";").ToList(), user, password, sshPort, serviceDir, logPathService);
                        }
                        Task.Delay(waitTime).Wait();

                        // start app server
                        if (connectionString == null)
                        {
                            // serverless mode (connectionString != null) does not need to start app server
                            appServerInUseList.ForEach(host => StartCollectMachineStatisticsTimer(host, user, password, sshPort, Util.MakeSureDirectoryExist(statisticFolder) + $"appserver{host}.txt", TimeSpan.FromSeconds(1)));
                            ShellHelper.StartAppServer(appServerInUseList, user, password, sshPort, azureSignalrConnectionStrings, logPathAppServer, useLocalSignalR, appSvrRoot);
                            Task.Delay(waitTime).Wait();
                        }

                        // start agents
                        privateIps.AgentPrivateIp.Split(";").ToList().ForEach(host => StartCollectMachineStatisticsTimer(host, user, password, sshPort, Path.Combine(Util.MakeSureDirectoryExist(statisticFolder), $"agent{host}.txt"), TimeSpan.FromSeconds(1)));
                        ShellHelper.StartRpcAgents(privateIps.AgentPrivateIp.Split(";").ToList(), user, password, sshPort, rpcPort, logPathAgent, agentRoot);Task.Delay(waitTime).Wait();

                        // start master
                        privateIps.MasterPrivateIp.Split(";").ToList().ForEach(host => StartCollectMachineStatisticsTimer(host, user, password, sshPort, Path.Combine(Util.MakeSureDirectoryExist(statisticFolder), $"master{host}.txt"), TimeSpan.FromSeconds(1)));
                        ShellHelper.StartRpcMaster(privateIps.MasterPrivateIp, privateIps.AgentPrivateIp.Split(";").ToList(),
                            user, password, sshPort, logPathMaster, serviceType, transportType, hubProtocol, scenario,
                            connection, concurrentConnection, duration, interval, pipeline, groupNum, overlap, combineFactor, messageSize,
                            serverUrl, suffix, masterRoot, sendToFixedClient, enableGroupJoinLeave,
                            bool.Parse(argsOption.StopSendIfLatencyBig), bool.Parse(argsOption.StopSendIfConnectionErrorBig),
                            connectionString);

                        if (argsOption.Regular)
                        {
                            // collect all logs
                            ShellHelper.CollectStatistics(hosts, user, password, sshPort, $"/home/{user}/logs/{resultRoot}/*.txt", Util.MakeSureDirectoryExist(logFolder));
                            // collect results from master
                            ShellHelper.CollectStatistics(privateIps.MasterPrivateIp.Split(";").ToList(), user, password, sshPort, $"/home/{user}/results/{resultRoot}/{masterSuffix}/*.txt", Util.MakeSureDirectoryExist(resultFolder));
                            // copy job config file
                            collector.CollectConfig(argsOption.JobConfigFileV2);
                        }
                        else
                        {
                            // collect all logs
                            ShellHelper.CollectStatistics(hosts, user, password, sshPort, $"/home/{user}/logs/{resultRoot}/", Util.MakeSureDirectoryExist(logFolder));
                            // collect results from master
                            ShellHelper.CollectStatistics(privateIps.MasterPrivateIp.Split(";").ToList(), user, password, sshPort, $"/home/{user}/results/{resultRoot}/", Util.MakeSureDirectoryExist(resultFolder));
                        }

                        if (neverStopAppServer)
                        {
                            ShellHelper.CollectStatistics(appServerInUseList, user, password, sshPort,
                                $"/home/{user}/logs/", Util.MakeSureDirectoryExist(logFolder));
                        }
                        // killall process to avoid wirting log
                        if (!debug) ShellHelper.KillAllDotnetProcess(hosts, remoteRepo, user, password, sshPort, repoRoot : localRepoRoot);

                        break;
                    }
            }

        }

        private static string GenerateSuffix(string agent)
        {
            return $"{agent}";
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
            timer.Elapsed += (sender, e) =>
            {
                ShellHelper.CollectMachineStatistics(host, user, password, sshPort, path);
            };
            timer.Start();

        }
    }
}
