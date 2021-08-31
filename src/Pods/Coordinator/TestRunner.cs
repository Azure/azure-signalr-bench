// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Coordinator.Entities;
using Azure.SignalRBench.Messages;
using Azure.SignalRBench.Storage;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Azure.SignalRBench.Coordinator
{
    public class TestRunner
    {
        private readonly Dictionary<string, SetClientRangeParameters> _clients =
            new Dictionary<string, SetClientRangeParameters>();

        private readonly ConcurrentDictionary<string, ReportClientStatusParameters> _clientStatus =
            new ConcurrentDictionary<string, ReportClientStatusParameters>();

        private readonly ILogger<TestRunner> _logger;

        private readonly List<RoundStatus>
            _roundStatusList = new List<RoundStatus>();

        private readonly List<string> _serverPods = new List<string>();
        private readonly Stopwatch _timer = new Stopwatch();
        private int _roundTotalConnected;
        private ITableAccessor<TestStatusEntity> _testStatusAccessor;
        private TestStatusEntity _testStatusEntity;

        private string _url = "http://localhost:8080/";
        private static int _scaleLock = 0;
        private static volatile string? _dir;
        private static volatile int _finishedJob = 0;
        private static volatile int _unitTotal = 0;
        private static volatile int _instanceTotal = 0;
        private static readonly object UnitLock = new object();

        public TestRunner(
            TestJob job,
            string podName,
            string redisConnectionString,
            IAksProvider aksProvider,
            IK8sProvider k8SProvider,
            SignalRProvider signalRProvider,
            IPerfStorage perfStorage,
            string defaultLocation,
            ILogger<TestRunner> logger)
        {
            Job = job;
            PodName = podName;
            RedisConnectionString = redisConnectionString;
            AksProvider = aksProvider;
            K8SProvider = k8SProvider;
            SignalRProvider = signalRProvider;
            PerfStorage = perfStorage;
            DefaultLocation = defaultLocation;
            _logger = logger;
        }

        public TestJob Job { get; }

        public string PodName { get; set; }

        public string RedisConnectionString { get; set; }

        public IAksProvider AksProvider { get; }

        public IK8sProvider K8SProvider { get; }

        public SignalRProvider SignalRProvider { get; }

        public IPerfStorage PerfStorage { get; }

        public string DefaultLocation { get; }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            if (Job.ServiceSetting.Length == 0)
            {
                _logger.LogWarning("Test job {testId}: No service configuration.", Job.TestId);
                return;
            }

            while (true)
            {
                await Task.Delay(2000);
                if (Job.Dir == null)
                {
                    break;
                }

                lock (UnitLock)
                {
                    if (_dir == null || Job.Dir == _dir)
                    {
                        _dir = Job.Dir;
                        break;
                    }
                }
            }

            _timer.Start();
            _testStatusAccessor =
                await PerfStorage.GetTableAsync<TestStatusEntity>(PerfConstants.TableNames.TestStatus);
            var pairs = Job.TestId.Split("--");
            Job.TestId = Job.TestId.Replace("--", "-");
            _testStatusEntity =
                await _testStatusAccessor.GetAsync(pairs[0], pairs[1]);
            var clientAgentCount = Job.ScenarioSetting.TotalConnectionCount;
            var clientPodCount = Job.PodSetting.ClientCount;
            _logger.LogInformation("Test job {testId}: Client pods count: {count}.", Job.TestId, clientPodCount);
            var serverPodCount = Job.PodSetting.ServerCount;
            _logger.LogInformation("Test job {testId}: Server pods count: {count}.", Job.TestId, serverPodCount);
            var nodeCount = clientPodCount + serverPodCount;
            _logger.LogInformation("Test job {testId}: Node count: {count}.", Job.TestId, nodeCount);
            var asrsConnectionStringsTask = PrepairAsrsInstancesAsync(cancellationToken);
            //leave this to autoscale. When ca is enabled, manual config won't work
            // await AksProvider.EnsureNodeCountAsync(NodePoolIndex, nodeCount, cancellationToken);
            using var messageClient = await MessageClient.ConnectAsync(RedisConnectionString, Job.TestId, PodName);
            await messageClient.WithHandlers(MessageHandler.CreateCommandHandler(Roles.Coordinator,
                Commands.Coordinator.ReportClientStatus, CollectClientStatus));
            CancellationTokenSource? scheduleCts = null;
            try
            {
                var asrsConnectionStrings = await asrsConnectionStringsTask;
                await UpdateTestStatus("Creating pods, autoscaling nodes..");
                await CreatePodsAsync(asrsConnectionStrings, clientAgentCount, clientPodCount, serverPodCount,
                    messageClient, cancellationToken);
                //        await UpdateTestStatus("Starting client connections");
                var i = 0;
                var suspiciousCount = 0;
                foreach (var round in Job.ScenarioSetting.Rounds)
                {
                    i++;
                    await UpdateTestStatus($"Round {i}: Connecting");
                    var totalConnectionDeltaThisRound = GetTotalConnectionDeltaCurrentRound(i);
                    scheduleCts = new CancellationTokenSource();
                    _ = ScheduleStateUpdate(i, _roundTotalConnected, scheduleCts.Token);
                    await SetClientRange(totalConnectionDeltaThisRound, clientPodCount, messageClient,
                        cancellationToken);
                    await StartClientConnectionsAsync(messageClient, cancellationToken);
                    scheduleCts.Cancel();
                    await Task.Delay(2000);
                    _clientStatus.Clear();
                    await SetScenarioAsync(messageClient, round, cancellationToken);
                    await UpdateTestStatus($"Round {i}: Testing");
                    await StartScenarioAsync(messageClient, cancellationToken);
                    await Task.Delay(TimeSpan.FromSeconds(round.DurationInSeconds), cancellationToken);
                    await StopScenarioAsync(messageClient, cancellationToken);
                    //wait for the last message to come back
                    await Task.Delay(5000);
                    suspiciousCount= await UpdateTestReports(round, _roundTotalConnected,suspiciousCount);
                }

                // await UpdateTestStatus("Stopping client connections");
                // await StopClientConnectionsAsync(messageClient, cancellationToken);
                await UpdateTestStatus("Test Finishes");
            }
            catch (Exception e)
            {
                await UpdateTestStatus("Testing Round failed ", false, e);
            }
            finally
            {
                if (scheduleCts != null && !scheduleCts.IsCancellationRequested)
                    scheduleCts.Cancel();
                _timer.Stop();
                try
                {
                    _logger.LogInformation("Test job {testId}: Removing service instances.", Job.TestId);
                    await Task.WhenAll(
                        from ss in Job.ServiceSetting
                        where ss.AsrsConnectionString == null
                        group ss by ss.Env
                        into env
                        select SignalRProvider.GetSignalRProvider(env.Key).DeleteResourceGroupAsync(Job.TestId));
                    _logger.LogInformation("Test job {testId}: Removing hashTable in redis.", Job.TestId);
                    await messageClient.DeleteHashTableAsync();
                    _logger.LogInformation("Test job {testId}: Removing client pods.", Job.TestId);
                    await K8SProvider.DeleteClientPodsAsync(Job.TestId);
                    _logger.LogInformation("Test job {testId}: Removing server pods.", Job.TestId);
                    await K8SProvider.DeleteServerPodsAsync(Job.TestId,
                        Job.TestMethod == TestCategory.AspnetCoreSignalRServerless||Job.TestMethod == TestCategory.RawWebsocket);
                }
                catch (Exception ignore)
                {
                    await UpdateTestStatus("Clean up failed", false, ignore);
                }
                finally
                {
                    lock (UnitLock)
                    {
                        if (Job.Dir != null)
                        {
                            _unitTotal -= Job.ServiceSetting[0].Size.Value;
                            _instanceTotal--;
                            _finishedJob++;
                            if (_finishedJob == Job.Total)
                            {
                                _finishedJob = 0;
                                _instanceTotal = 0;
                                _dir = null;
                            }
                        }
                    }
                }
            }
        }

        public int GetTotalConnectionDeltaCurrentRound(int i)
        {
            double percent = 1;
            if (i < Job.ScenarioSetting.TotalConnectionRound)
                percent = (double) i / Job.ScenarioSetting.TotalConnectionRound;

            var totalThisRound = (int) Math.Ceiling(Job.ScenarioSetting.TotalConnectionCount * percent);
            //Format the active. Assume only one client setting.
            var old = Job.ScenarioSetting.Rounds[i - 1].ClientSettings[0].Count;
            Job.ScenarioSetting.Rounds[i - 1].ClientSettings[0].Count = old < totalThisRound ? old : totalThisRound;
            var result = totalThisRound - _roundTotalConnected;
            _roundTotalConnected = totalThisRound;
            return result;
        }

        public async Task ScheduleStateUpdate(int i, int totalConnectionThisRound, CancellationToken ctx)
        {
            _logger.LogInformation("Start to record client connection status...");
            var _totalConnected = 0;
            while (!ctx.IsCancellationRequested)
            {
                _totalConnected = _clientStatus.Select(p =>
                {
                    _logger.LogInformation(
                        $"{p.Key} connected:{p.Value.ConnectedCount} , reconnecting:{p.Value.ReconnectingCount} , totalReconnected:{p.Value.TotalReconnectCount},time: {p.Value.Time}");
                    return p.Value.ConnectedCount;
                }).Sum();
                var totalReconnectiong = _clientStatus.Select(p => p.Value.ReconnectingCount).Sum();
                var totalReconnectedCount = _clientStatus.Select(p => p.Value.TotalReconnectCount).Sum();
                _logger.LogInformation(
                    $"\n [Total] reported client count:[ {_clientStatus.Count} ], Reconnected:{totalReconnectedCount} , Reconnecting:{totalReconnectiong}, connected:{_totalConnected}");
                _logger.LogInformation("Total connected:" + _totalConnected + "\n");
                await UpdateTestStatus($"Round {i}: Connected:{_totalConnected}/{totalConnectionThisRound}");
                if (_totalConnected == totalConnectionThisRound)
                {
                    _logger.LogInformation("All connections established");
                    return;
                }

                if (_totalConnected > totalConnectionThisRound)
                    _logger.LogError($"{_totalConnected} is bigger than {totalConnectionThisRound}");

                await Task.Delay(2000);
            }
        }

        public async Task UpdateTestStatus(string currentStatus, bool healthy = true, Exception? e = default)
        {
            var sec = _timer.ElapsedMilliseconds / 1000;
            _testStatusEntity.Status = currentStatus + " [" + sec + "sec]";
            _testStatusEntity.Healthy = healthy;
            if (healthy)
            {
                _logger.LogInformation(currentStatus);
            }
            else
            {
                _logger.LogError(e, currentStatus);
                if (e != null)
                    _testStatusEntity.ErrorInfo += "   \n  \n     " + e;
            }

            await _testStatusAccessor.UpdateAsync(_testStatusEntity);
        }

        private async Task<int> UpdateTestReports(RoundSetting round, int totalConnectionsThisRound, int suspiciousCount)
        {
            var roundStatus = new RoundStatus
            {
                ConnectedCount = 0,
                Latency = new Dictionary<LatencyClass, int>(),
                MessageRecieved = 0,
                MessageSent = 0,
                ReconnectingCount = 0,
                TotalReconnectCount = 0,
                ExpectedRecievedMessageCount = 0,
                //for now, there is only one clientsetting in one round
                ActiveConnection = round.ClientSettings[0].Count,
                RoundConnected = totalConnectionsThisRound
            };
            foreach (var v in _clientStatus.Values)
            {
                roundStatus.ConnectedCount += v.ConnectedCount;
                roundStatus.MessageRecieved += v.MessageRecieved;
                roundStatus.MessageSent += v.MessageSent;
                roundStatus.ReconnectingCount += v.ReconnectingCount;
                roundStatus.TotalReconnectCount += v.TotalReconnectCount;
                roundStatus.ExpectedRecievedMessageCount += v.ExpectedRecievedMessageCount;
                foreach (var kv in v.Latency)
                {
                    if (!roundStatus.Latency.ContainsKey(kv.Key))
                        roundStatus.Latency[kv.Key] = 0;
                    roundStatus.Latency[kv.Key] = roundStatus.Latency[kv.Key] + kv.Value;
                }
            }

            //ugly code. Just to avoid to much suspicious result
            if (!roundStatus.Check())
            {
                _testStatusEntity.Check = "Suspicious";
                suspiciousCount++;
            }
            else
            {
                if (suspiciousCount > 0)
                    suspiciousCount--;
                if (suspiciousCount == 0)
                    _testStatusEntity.Check = null;
            }
    
            _roundStatusList.Add(roundStatus);
            _testStatusEntity.Report = JsonConvert.SerializeObject(_roundStatusList);
            await _testStatusAccessor.UpdateAsync(_testStatusEntity);
            return suspiciousCount;
        }

        private async Task<string[]> PrepairAsrsInstancesAsync(CancellationToken cancellationToken)
        {
            var asrsConnectionStrings = new string[Job.ServiceSetting.Length];
            for (var i = 0; i < Job.ServiceSetting.Length; i++)
            {
                var ss = Job.ServiceSetting[i];
                await UpdateTestStatus("task queueing..");
                while (Job.Dir != null)
                {
                    await Task.Delay(StaticRandom.Next(5000));
                    lock (UnitLock)
                    {
                        if (_unitTotal >= Job.ServiceSetting[0].UnitLimit ||
                            _instanceTotal >= Job.ServiceSetting[0].InstanceLimit) continue;
                        _unitTotal += ss.Size.Value;
                        _instanceTotal++;
                        break;
                    }
                }

                await UpdateTestStatus("Creating SignalR instance..");
                if (ss.AsrsConnectionString == null)
                    asrsConnectionStrings[i] =
                        await CreateAsrsAsync(ss, PerfConstants.ConfigurationKeys.PerfV2 + "-" + Job.TestId + '-' + i,
                            cancellationToken);
                else
                    asrsConnectionStrings[i] = ss.AsrsConnectionString;
            }

            return asrsConnectionStrings;
        }

        private async Task<string> CreateAsrsAsync(ServiceSetting ss, string name, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Test job {testId}: Creating SignalR service instance.", Job.TestId);
            var signalRProvider = SignalRProvider.GetSignalRProvider(ss.Env);
            await signalRProvider.CreateResourceGroupAsync(Job.TestId);
            await signalRProvider.CreateInstanceAsync(
                Job.TestId,
                name,
                ss.Location ?? DefaultLocation,
                ss.Tier ?? "Standard",
                ss.Size ?? 1, ss.Tags,
                Job.TestMethod.GetServiceMode(),
                cancellationToken);
            _logger.LogInformation("Test job {testId}: SignalR service instance created.", Job.TestId);
            _logger.LogInformation("Test job {testId}: Retrieving SignalR service connection string.", Job.TestId);
            var result =
                await signalRProvider.GetKeyAsync(Job.TestId, name, cancellationToken);
            _logger.LogInformation("Test job {testId}: SignalR service connection string retrieved.", Job.TestId);
            return result;
        }

        private Task CollectClientStatus(CommandMessage msg)
        {
            var status = msg.Parameters.ToObject<ReportClientStatusParameters>();
            _clientStatus[msg.Sender] = status;
            return Task.CompletedTask;
        }


        private Func<CommandMessage, Task> GetReportReady(
            int clientAgentCount,
            int clientPodCount,
            int serverPodCount,
            out Task clientPodsReady,
            out Task serverPodsReady)
        {
            var clientReadyCount = 0;
            var serverReadyCount = 0;
            var clientPodsReadyTcs = new TaskCompletionSource<object?>();
            var serverPodsReadyTcs = new TaskCompletionSource<object?>();
            clientPodsReady = clientPodsReadyTcs.Task;
            serverPodsReady = serverPodCount <= 0 ? Task.CompletedTask : serverPodsReadyTcs.Task;
            return m =>
            {
                var p = m.Parameters?.ToObject<ReportReadyParameters>();
                if (p == null)
                    // todo: log.
                    return Task.CompletedTask;

                var sec = _timer.ElapsedMilliseconds / 1000;
                if (p.Role == Roles.Clients)
                {
                    clientReadyCount++;
                    _clients[m.Sender] =
                        new SetClientRangeParameters();
                    _logger.LogInformation($"client: {m.Sender} is ready. {clientReadyCount} is ready. sec:{sec}");
                    if (clientReadyCount == clientPodCount)
                        clientPodsReadyTcs.TrySetResult(null);
                    else if (clientReadyCount > clientPodCount)
                        _logger.LogError($"More client pods are created:{clientReadyCount}/{clientPodCount}");
                }
                else if (p.Role == Roles.AppServers)
                {
                    serverReadyCount++;
                    _logger.LogInformation($"server: {m.Sender} is ready. {serverReadyCount} is ready. sec:{sec}");
                    if (serverReadyCount == serverPodCount)
                        serverPodsReadyTcs.TrySetResult(null);
                    else if (serverReadyCount > serverPodCount)
                        _logger.LogError($"More server pods are created:{serverReadyCount}/{serverPodCount}");
                }

                return Task.CompletedTask;
            };
        }

        private async Task SetClientRange(
            int clientAgentCount,
            int clientPodCount,
            MessageClient messageClient,
            CancellationToken cancellationToken
        )
        {
            var clientReadyCount = 0;
            var countPerPod = clientAgentCount / clientPodCount;
            var keys = new List<string>(_clients.Keys);
            foreach (var k in keys)
            {
                clientReadyCount++;
                if (clientReadyCount < clientPodCount)
                    _clients[k] =
                        new SetClientRangeParameters
                        {
                            StartIdTruncated = (clientReadyCount - 1) * countPerPod,
                            LocalCountDelta = countPerPod,
                            TotalCountDelta = clientAgentCount
                        };
                else if (clientReadyCount == clientPodCount)
                    _clients[k] =
                        new SetClientRangeParameters
                        {
                            StartIdTruncated = (clientReadyCount - 1) * countPerPod,
                            LocalCountDelta = clientAgentCount - (clientReadyCount - 1) * countPerPod,
                            TotalCountDelta = clientAgentCount
                        };
            }

            var clientCompleteTask = await messageClient.GetWhenAllAckAsync(
                _clients.Keys,
                Commands.Clients.SetClientRange,
                cancellationToken);
            await Task.WhenAll(_clients.Select(pair => messageClient.SetClientRangeAsync(pair.Key, pair.Value)));
            await clientCompleteTask;
        }

        private async Task CreatePodsAsync(
            string[] asrsConnectionStrings,
            int clientAgentCount,
            int clientPodCount,
            int serverPodCount,
            MessageClient messageClient,
            CancellationToken cancellationToken)
        {
            while (Interlocked.CompareExchange(ref _scaleLock, 1, 0) == 1)
            {
                await UpdateTestStatus("Deployment queueing..");
                _logger.LogInformation("Wait for deployment to be ready. Avoid race condition...");
                await Task.Delay(5000);
            }

            try
            {
                await UpdateTestStatus("Creating deployment..");
                await messageClient.WithHandlers(
                    MessageHandler.CreateCommandHandler(
                        Roles.Coordinator,
                        Commands.Coordinator.ReportReady,
                        GetReportReady(clientAgentCount, clientPodCount, serverPodCount, out var clientPodReady,
                            out var serverPodReady)));

                _logger.LogInformation("Test job {testId}: Creating server pods.", Job.TestId);
                _url = await K8SProvider.CreateServerPodsAsync(Job.TestId, asrsConnectionStrings,
                    serverPodCount, Job.TestMethod, cancellationToken);
                _logger.LogInformation("Test job {testId}: Creating client pods.", Job.TestId);
                await K8SProvider.CreateClientPodsAsync(Job.TestId, Job.TestMethod, clientPodCount, cancellationToken);

                await Task.WhenAll(
                    Task.Run(async () =>
                    {
                        await clientPodReady;
                        _logger.LogInformation("Test job {testId}: Client pods ready.", Job.TestId);
                    }),
                    Task.Run(async () =>
                    {
                        await serverPodReady;
                        _logger.LogInformation("Test job {testId}: Server pods ready.", Job.TestId);
                    }));
            }
            finally
            {
                _logger.LogInformation("Reset scale control to 0.");
                Interlocked.CompareExchange(ref _scaleLock, 0, 1);
            }
        }

        private async Task StartClientConnectionsAsync(
            MessageClient messageClient,
            CancellationToken cancellationToken)
        {
            var task = await messageClient.GetWhenAllAckAsync(
                _clients.Keys,
                Commands.Clients.StartClientConnections,
                cancellationToken);
            //dirty logic, reset group count, assume only one group
            if (Job.ScenarioSetting.GroupDefinitions.Length > 0)
            {
                var groupSize = Job.ScenarioSetting.GroupDefinitions[0].GroupSize;
                Job.ScenarioSetting.GroupDefinitions[0].GroupCount =
                    _roundTotalConnected / groupSize + (_roundTotalConnected % groupSize == 0 ? 0 : 1);
            }

            await messageClient.StartClientConnectionsAsync(
                new StartClientConnectionsParameters
                {
                    ClientLifetime = Job.ScenarioSetting.ClientLifetime,
                    GroupDefinitions = Job.ScenarioSetting.GroupDefinitions,
                    IsAnonymous = Job.ScenarioSetting.IsAnonymous,
                    Protocol = Job.ScenarioSetting.Protocol,
                    Rate = Job.ScenarioSetting.Rate / _clients.Count,
                    Url = _url
                });
            await task;
            _logger.LogInformation(" start  client connections acked.");
        }

        private async Task SetScenarioAsync(
            MessageClient messageClient,
            RoundSetting round,
            CancellationToken cancellationToken)
        {
            var task = await messageClient.GetWhenAllAckAsync(
                _clients.Keys,
                Commands.Clients.SetScenario,
                cancellationToken);
            await messageClient.SetScenarioAsync(
                new SetScenarioParameters
                {
                    Scenarios = Array.ConvertAll(
                        round.ClientSettings,
                        cs =>
                        {
                            var sd = new ScenarioDefinition {ClientBehavior = cs.Behavior};
                            if (cs.Behavior == ClientBehavior.GroupBroadcast)
                                sd.SetDetail(
                                    new GroupClientBehaviorDetailDefinition
                                    {
                                        Count = cs.Count,
                                        Interval = TimeSpan.FromMilliseconds(cs.IntervalInMilliseconds),
                                        MessageSize = cs.MessageSize,
                                        GroupFamily = cs.GroupFamily ??
                                                      throw new InvalidDataException("Group family is required.")
                                    });
                            else
                                sd.SetDetail(
                                    new ClientBehaviorDetailDefinition
                                    {
                                        Count = cs.Count,
                                        Interval = TimeSpan.FromMilliseconds(cs.IntervalInMilliseconds),
                                        MessageSize = cs.MessageSize
                                    });

                            return sd;
                        })
                });
            await task;
            _logger.LogInformation(" set scenario acked.");
        }

        private async Task StartScenarioAsync(
            MessageClient messageClient,
            CancellationToken cancellationToken)
        {
            var task = await messageClient.GetWhenAllAckAsync(
                _clients.Keys,
                Commands.Clients.StartScenario,
                cancellationToken);
            await messageClient.StartScenarioAsync(
                new StartScenarioParameters()
                {
                    CoordinatorTime = DateTime.UtcNow.Ticks
                });
            await task;
            _logger.LogInformation(" Start scenario acked.");
        }

        private async Task StopScenarioAsync(
            MessageClient messageClient,
            CancellationToken cancellationToken)
        {
            var task = await messageClient.GetWhenAllAckAsync(
                _clients.Keys,
                Commands.Clients.StopScenario,
                cancellationToken);
            await messageClient.StopScenarioAsync(
                new StopScenarioParameters());
            await task;
            _logger.LogInformation(" Stop scenario acked.");
        }

        private async Task StopClientConnectionsAsync(
            MessageClient messageClient,
            CancellationToken cancellationToken)
        {
            var task = await messageClient.GetWhenAllAckAsync(
                _clients.Keys,
                Commands.Clients.StopClientConnections,
                cancellationToken);
            await messageClient.StopClientConnectionsAsync(
                new StopClientConnectionsParameters
                {
                    Rate = Job.ScenarioSetting.Rate / _clients.Count
                });
            await task;
            _logger.LogInformation(" Stop client connections acked.");
        }
    }
}