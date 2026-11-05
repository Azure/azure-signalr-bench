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
using Newtonsoft.Json.Linq;

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

        private ClientCommandHistory _history = new ClientCommandHistory();
        private ClientCommandRound _currentRound;
        
        private string _url = "http://localhost:8080/";
        private static int _scaleLock = 0;
        private static Dictionary<string,directory> _dirs= new Dictionary<string,directory >();
        private static readonly object UnitLock = new object();
        private Action _cb;
        private readonly object _controlLock = new object();
        private readonly Dictionary<string, (string Role, string InstanceId)> _participants =
            new Dictionary<string, (string, string)>(StringComparer.Ordinal);
        private readonly CancellationTokenSource _internalAbort = new CancellationTokenSource();
        private Exception? _internalFailure;
        private Exception? _outcomeError;
        private volatile string? _terminalCheck;
        private bool _outcomeSealed;
        private CancellationToken _externalToken;
        private string _phase = "initialization";
        private IDisposable? _readyScope;
        private int _reservedUnits;
        private int _reservedInstances;

        public TestRunner(
            TestJob job,
            string podName,
            string redisConnectionString,
            IAksProvider aksProvider,
            IK8sProvider k8SProvider,
            SignalRProvider signalRProvider,
            IPerfStorage perfStorage,
            string defaultLocation,
            Action cb,
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
            _cb = cb;
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

        public CancellationTokenSource Cts { get; set; } = new CancellationTokenSource();

        private async Task InitializeStatusAsync()
        {
            _testStatusAccessor =
                await PerfStorage.GetTableAsync<TestStatusEntity>(PerfConstants.TableNames.TestStatus);
            var pairs = Job.TestId.Split("--");
            _testStatusEntity = await _testStatusAccessor.GetAsync(pairs[0], pairs[1])
                ?? throw new InvalidDataException($"Missing test status for {Job.TestId}.");
        }

        public async Task RunAsync(CancellationToken cancellationToken0)
        {
            if (Job.Dir != null)
            {
                lock (UnitLock)
                {
                    _dirs.TryAdd(Job.Dir, new directory());
                }
            }  
            
            using var external = CancellationTokenSource.CreateLinkedTokenSource(Cts.Token, cancellationToken0);
            _externalToken = external.Token;
            using var work = CancellationTokenSource.CreateLinkedTokenSource(external.Token, _internalAbort.Token);
            var cancellationToken = work.Token;
            _timer.Start();
            var clientAgentCount = Job.ScenarioSetting.TotalConnectionCount;
            var clientPodCount = Job.PodSetting.ClientCount;
            _logger.LogInformation("Test job {testId}: Client pods count: {count}.", Job.TestId, clientPodCount);
            var serverPodCount = Job.PodSetting.ServerCount;
            _logger.LogInformation("Test job {testId}: Server pods count: {count}.", Job.TestId, serverPodCount);
            var nodeCount = clientPodCount + serverPodCount;
            _logger.LogInformation("Test job {testId}: Node count: {count}.", Job.TestId, nodeCount);
            MessageClient? messageClient = null;
            IDisposable? statusScope = null;
            CancellationTokenSource? scheduleCts = null;
            Task? scheduleTask = null;
            try
            {
                await InitializeStatusAsync();
                if (Job.ServiceSetting.Length == 0)
                    throw new InvalidDataException($"Test {Job.TestId}: no service configuration.");
                var asrsConnectionStrings = await PrepairAsrsInstancesAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                messageClient = await MessageClient.ConnectAsync(RedisConnectionString, Job.TestId, PodName);
                statusScope = await messageClient.WithHandlers(MessageHandler.CreateCommandHandler(Roles.Coordinator,
                    Commands.Coordinator.ReportClientStatus, CollectClientStatus));
                await UpdateTestStatus("Creating pods, autoscaling nodes..");
                await CreatePodsAsync(asrsConnectionStrings, clientAgentCount, clientPodCount, serverPodCount,
                    messageClient, cancellationToken);
                //        await UpdateTestStatus("Starting client connections");
                var i = 0;
                var suspiciousCount = 0;
                foreach (var round in Job.ScenarioSetting.Rounds)
                {
                    i++;
                    _phase = $"round {i}: connecting";
                    await UpdateTestStatus($"Round {i}: Connecting");
                    var totalConnectionDeltaThisRound = GetTotalConnectionDeltaCurrentRound(i);
                    scheduleCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    scheduleTask = ScheduleStateUpdate(i, _roundTotalConnected, scheduleCts.Token);
                    await SetClientRange(totalConnectionDeltaThisRound, clientPodCount, messageClient,
                        cancellationToken);
                    await StartClientConnectionsAsync(messageClient, cancellationToken);
                    await StopProgressAsync(scheduleCts, scheduleTask);
                    scheduleCts.Dispose();
                    scheduleCts = null;
                    scheduleTask = null;
                    await Task.Delay(2000, cancellationToken);
                    _clientStatus.Clear();
                    _phase = $"round {i}: setting scenario";
                    await SetScenarioAsync(messageClient, round, cancellationToken);
                    await UpdateTestStatus($"Round {i}: Testing");
                    _phase = $"round {i}: scenario";
                    await StartScenarioAsync(messageClient, i == Job.ScenarioSetting.Rounds.Length ,cancellationToken);
                    await Task.Delay(TimeSpan.FromSeconds(round.DurationInSeconds), cancellationToken);
                    await StopScenarioAsync(messageClient, cancellationToken);
                    //wait for the last message to come back
                    await Task.Delay(5000, cancellationToken);
                    suspiciousCount = await UpdateTestReports(round, _roundTotalConnected, suspiciousCount);
                }

                if (_testStatusEntity.LongRun)
                {
                    // Trigger long run 
                    await StartScenarioAsync(messageClient, false, cancellationToken);
                    SealOutcome(null, external.Token);
                    if (_outcomeError != null)
                        throw _outcomeError;
                    await ArchiveLongRunTestStatus();
                }
                else
                {
                    await StopTestAsync(messageClient, cancellationToken);
                    // await UpdateTestStatus("Stopping client connections");
                    // await StopClientConnectionsAsync(messageClient, cancellationToken);
                }
                SealOutcome(null, external.Token);
            }
            catch (Exception e)
            {
                SealOutcome(e, external.Token);
                _logger.LogError(_outcomeError, "Test job {testId}: workflow stopped ({outcome}).",
                    Job.TestId, _terminalCheck);
            }
            finally
            {
                await CleanupAsync(messageClient, statusScope, scheduleCts, scheduleTask);
            }
        }

        private async Task CleanupAsync(MessageClient? messageClient, IDisposable? statusScope,
            CancellationTokenSource? scheduleCts, Task? scheduleTask)
        {
            try
            {
                await StopProgressAsync(scheduleCts, scheduleTask);
            }
            catch (Exception e)
            {
                RecordCleanupFailure(e);
            }
            scheduleCts?.Dispose();
            try
            {
                _readyScope?.Dispose();
            }
            catch (Exception e)
            {
                RecordCleanupFailure(e);
            }
            try
            {
                statusScope?.Dispose();
            }
            catch (Exception e)
            {
                RecordCleanupFailure(e);
            }
            _timer.Stop();
            try
            {
                if (_testStatusEntity != null && _testStatusEntity.JobState != TestState.Longrun.ToString())
                {
                    await UpdateTestStatus(OutcomeStatus(), e: _outcomeError, testState: TestState.Cleaning);
                    _logger.LogInformation("Test job {testId}: Removing service instances.", Job.TestId);
                    await Task.WhenAll(
                        from ss in Job.ServiceSetting
                        where ss.AsrsConnectionString == null
                        group ss by ss.Env
                        into env
                        select SignalRProvider.GetSignalRProvider(env.Key).DeleteResourceGroupAsync(Job.TestId));
                    _logger.LogInformation("Test job {testId}: Removing hashTable in redis.", Job.TestId);
                    if (messageClient != null)
                        await messageClient.DeleteHashTableAsync();
                    _logger.LogInformation("Test job {testId}: Removing client pods.", Job.TestId);
                    await K8SProvider.DeleteClientPodsAsync(Job.TestId);
                    _logger.LogInformation("Test job {testId}: Removing server pods.", Job.TestId);
                    await K8SProvider.DeleteServerPodsAsync(Job.TestId,
                        Job.TestMethod == TestCategory.AspnetCoreSignalRServerless ||
                        Job.TestMethod == TestCategory.RawWebsocket);
                    await UpdateTestStatus(OutcomeStatus(), testState: TestState.Cleaned);
                }
            }
            catch (Exception e)
            {
                RecordCleanupFailure(e);
                if (_testStatusEntity != null)
                    await UpdateTestStatus(OutcomeStatus() + "; cleanup failed", e: e, testState: TestState.Cleaning);
                else
                    _logger.LogError(e, "Test job {testId}: initialization/cleanup failed.", Job.TestId);
            }
            finally
            {
                try
                {
                    messageClient?.Dispose();
                }
                catch (Exception e)
                {
                    RecordCleanupFailure(e);
                    if (_testStatusEntity != null)
                        await UpdateTestStatus(OutcomeStatus() + "; cleanup failed", e: e, testState: TestState.Cleaning);
                }
                finally
                {
                    _cb();
                    ReleaseDirectoryQuota();
                }
            }
        }

        private void ReleaseDirectoryQuota()
        {
            lock (UnitLock)
            {
                if (Job.Dir != null && _dirs.TryGetValue(Job.Dir, out var dirConfig) &&
                    _testStatusEntity?.JobState != TestState.Longrun.ToString())
                {
                    dirConfig._unitTotal -= _reservedUnits;
                    dirConfig._instanceTotal -= _reservedInstances;
                    dirConfig._finishedJob++;
                    if (dirConfig._finishedJob == Job.Total)
                        _dirs.Remove(Job.Dir);
                }
            }
        }

        private void SealOutcome(Exception? error, CancellationToken externalToken)
        {
            lock (_controlLock)
            {
                if (_outcomeSealed)
                    return;
                _outcomeSealed = true;
                _outcomeError = _internalFailure ?? error;
                if (_internalFailure == null && externalToken.IsCancellationRequested &&
                    (error == null || error is OperationCanceledException))
                {
                    _terminalCheck = "Cancelled";
                    _outcomeError = error ?? new OperationCanceledException(externalToken);
                }
                else if (_outcomeError != null)
                    _terminalCheck = "Failed";
            }
        }

        private void RecordCleanupFailure(Exception error)
        {
            lock (_controlLock)
            {
                _outcomeSealed = true;
                _terminalCheck ??= "Failed";
                _outcomeError ??= error;
            }
        }

        private string OutcomeStatus() => _terminalCheck == "Cancelled" ? "Test cancelled" :
            _terminalCheck == "Failed" ? "Test failed" : "Test Finishes";

        private static async Task StopProgressAsync(CancellationTokenSource? source, Task? task)
        {
            source?.Cancel();
            if (task == null)
                return;
            try
            {
                // Join the progress writer before later status and report writes.
                await task;
            }
            catch (OperationCanceledException) when (source?.IsCancellationRequested == true)
            {
            }
        }
        
        public async Task StopAsync()
        {
            Cts.Cancel();
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

                await Task.Delay(2000, ctx);
            }
        }

        public async Task UpdateTestStatus(string currentStatus, bool? healthy = null, Exception? e = default,
            TestState testState = TestState.InProgress)
        {
            var sec = _timer.ElapsedMilliseconds / 1000;
            _testStatusEntity.Status = currentStatus + " [" + sec + "sec]";
            if (healthy == false || _terminalCheck != null)
                _testStatusEntity.Healthy = false;
            if (_terminalCheck != null)
                _testStatusEntity.Check = _terminalCheck;
            _testStatusEntity.JobState = testState.ToString();
            if (e != null)
                _testStatusEntity.ErrorInfo += "   \n  \n     " + e;
            if (_testStatusEntity.Healthy)
                _logger.LogInformation(currentStatus);
            else
                _logger.LogError(e, currentStatus);
            await _testStatusAccessor.UpdateAsync(_testStatusEntity);
        }

        public async Task ArchiveLongRunTestStatus()
        {
            try
            {
                _testStatusEntity.Status = "In long run";
                _testStatusEntity.JobState = TestState.Longrun.ToString();
                _testStatusEntity.LongRunContext = JsonConvert.SerializeObject(_history);
                await _testStatusAccessor.UpdateAsync(_testStatusEntity);
            }
            catch (Exception e)
            {
                RecordCleanupFailure(e);
                _testStatusEntity.JobState = TestState.Cleaning.ToString();
                throw;
            }
        }

        private async Task<int> UpdateTestReports(RoundSetting round, int totalConnectionsThisRound,
            int suspiciousCount)
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
                roundStatus.ClientReceivedServerAckCount += v.ClientReceivedServerAckCount;
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
                if (_terminalCheck == null)
                    _testStatusEntity.Check = "Suspicious";
                suspiciousCount++;
            }
            else
            {
                if (suspiciousCount > 0)
                    suspiciousCount--;
                if (suspiciousCount == 0 && _terminalCheck == null)
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
                var testJobDelay = false;
                while (Job.Dir != null)
                {
                    await Task.Delay(StaticRandom.Next(5000), cancellationToken);
                    lock (UnitLock)
                    {
                        var dirConfig=_dirs[Job.Dir];
                        _logger.LogInformation(
                            "Test job {testId}: Queue check - Dir={dir}, unitTotal={unitTotal}, instanceTotal={instanceTotal}, UnitLimit={unitLimit}, InstanceLimit={instanceLimit}",
                            Job.TestId, Job.Dir, dirConfig._unitTotal, dirConfig._instanceTotal,
                            Job.ServiceSetting[0].UnitLimit, Job.ServiceSetting[0].InstanceLimit);
                        if (dirConfig._unitTotal >= Job.ServiceSetting[0].UnitLimit ||
                            dirConfig._instanceTotal >= Job.ServiceSetting[0].InstanceLimit)
                        {
                            _logger.LogInformation("Test job {testId}: Queue blocked, waiting...", Job.TestId);
                            continue;
                        }
                        dirConfig._unitTotal += ss.Size.Value;
                        dirConfig._instanceTotal++;
                        _reservedUnits += ss.Size.Value;
                        _reservedInstances++;
                        testJobDelay = true;
                        _logger.LogInformation("Test job {testId}: Queue acquired, proceeding.", Job.TestId);
                        break;
                    }
                }

                if (testJobDelay)
                {
                    // Task in one dir normally use the same race. Give enough time to finish
                    await Task.Delay(30000, cancellationToken);
                }

                await UpdateTestStatus("Creating SignalR instance..");
                if (ss.AsrsConnectionString == null)
                    asrsConnectionStrings[i] =
                        await CreateAsrsAsync(ss, PerfConstants.ConfigurationKeys.PerfV2 + "-" + Job.TestId + '-' + i,
                            cancellationToken);
                else
                    asrsConnectionStrings[i] = Job.TestMethod.UsesSignalRManagedIdentity()
                        ? string.Join(" ", SignalRManagedIdentity.ParseEndpoints(ss.AsrsConnectionString)
                            .Select(endpoint => endpoint.AbsoluteUri))
                        : ss.AsrsConnectionString;
            }

            return asrsConnectionStrings;
        }

        private async Task<string> CreateAsrsAsync(ServiceSetting ss, string name, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Test job {testId}: Creating SignalR service instance.", Job.TestId);
            var signalRProvider = SignalRProvider.GetSignalRProvider(ss.Env);
            await signalRProvider.CreateResourceGroupAsync(Job.TestId);
            var endpoint = await signalRProvider.CreateInstanceAsync(
                Job.TestId,
                name,
                ss.Location ?? DefaultLocation,
                ss.Tier ?? "Standard",
                ss.Size ?? 1, ss.Tags,
                Job.TestMethod.GetServiceMode(),
                cancellationToken);
            _logger.LogInformation("Test job {testId}: SignalR service instance created.", Job.TestId);
            if (Job.TestMethod.UsesSignalRManagedIdentity())
            {
                return endpoint;
            }

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
            CancellationToken cancellationToken,
            out Task clientPodsReady,
            out Task serverPodsReady)
        {
            _phase = "Ready";
            var clientPodsReadyTcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var serverPodsReadyTcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (clientPodCount <= 0)
                clientPodsReadyTcs.TrySetResult(null);
            if (serverPodCount <= 0)
                serverPodsReadyTcs.TrySetResult(null);
            clientPodsReady = clientPodsReadyTcs.Task.WaitAsync(cancellationToken);
            serverPodsReady = serverPodsReadyTcs.Task.WaitAsync(cancellationToken);
            return m =>
            {
                try
                {
                    var p = m.Parameters?.ToObject<ReportReadyParameters>();
                    Exception? failure = null;
                    lock (_controlLock)
                    {
                        if (_outcomeSealed || _internalFailure != null || _externalToken.IsCancellationRequested)
                            return Task.CompletedTask;
                        if (p == null || string.IsNullOrWhiteSpace(p.InstanceId) ||
                            string.IsNullOrWhiteSpace(m.Sender) ||
                            (p.Role != Roles.Clients && p.Role != Roles.AppServers))
                            failure = new InvalidDataException(
                                $"Test {Job.TestId}, phase {_phase}: unsupported Ready protocol or invalid sender/role from '{m.Sender}'; InstanceId is required.");
                        else if (_participants.TryGetValue(m.Sender, out var previous))
                        {
                            if (previous.Role != p.Role || previous.InstanceId != p.InstanceId)
                                failure = new InvalidDataException(
                                    $"Test {Job.TestId}, phase {_phase}: participant {m.Sender} changed instance/role: {previous.Role}/{previous.InstanceId} -> {p.Role}/{p.InstanceId}.");
                        }
                        else
                        {
                            var count = _participants.Values.Count(value => value.Role == p.Role);
                            var expected = p.Role == Roles.Clients ? clientPodCount : serverPodCount;
                            if (count >= expected)
                                failure = new InvalidDataException(
                                    $"Test {Job.TestId}, phase {_phase}: unexpected Ready sender {m.Sender} ({p.Role}); expected {expected} participants.");
                            else
                            {
                                _participants.Add(m.Sender, (p.Role, p.InstanceId));
                                if (p.Role == Roles.Clients)
                                    _clients.Add(m.Sender, new SetClientRangeParameters());
                                _logger.LogInformation("Test {testId}: {role} {sender} is ready ({count}/{expected}).",
                                    Job.TestId, p.Role, m.Sender, count + 1, expected);
                                if (count + 1 == expected)
                                    (p.Role == Roles.Clients ? clientPodsReadyTcs : serverPodsReadyTcs).TrySetResult(null);
                            }
                        }
                        if (failure != null)
                            _internalFailure = failure;
                    }
                    if (failure != null)
                        _internalAbort.Cancel();
                }
                catch (Exception e)
                {
                    AbortRun(new InvalidDataException($"Test {Job.TestId}, phase {_phase}: invalid Ready.", e));
                }
                return Task.CompletedTask;
            };
        }

        private void AbortRun(Exception failure)
        {
            lock (_controlLock)
            {
                if (_outcomeSealed || _internalFailure != null || _externalToken.IsCancellationRequested)
                    return;
                _internalFailure = failure;
            }
            _internalAbort.Cancel();
        }

        private async Task SetClientRange(
            int clientAgentCount,
            int clientPodCount,
            IMessageClient messageClient,
            CancellationToken cancellationToken
        )
        {
            var clientReadyCount = 0;
            var countPerPod = clientAgentCount / clientPodCount;
            // Ready has fixed the client set; only this workflow updates the ranges.
            var keys = _clients.Keys.ToArray();
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
            var clients = _clients.ToArray();
            _currentRound = new ClientCommandRound();
            foreach (var pair in clients)
            {
                _currentRound.SetClientRangeParameters[pair.Key] = pair.Value;
            }
            await Task.WhenAll(clients.Select(pair => SendAndWaitAsync(messageClient, pair.Key,
                Commands.Clients.SetClientRange, pair.Value, new[] { pair.Key }, cancellationToken)));
        }

        private async Task CreatePodsAsync(
            string[] asrsConnectionStrings,
            int clientAgentCount,
            int clientPodCount,
            int serverPodCount,
            MessageClient messageClient,
            CancellationToken cancellationToken)
        {
            // while (Interlocked.CompareExchange(ref _scaleLock, 1, 0) == 1)
            // {
            //     await UpdateTestStatus("Deployment queueing..");
            //     _logger.LogInformation("Wait for deployment to be ready. Avoid race condition...");
            //     await Task.Delay(5000);
            // }

            await UpdateTestStatus("Creating deployment..");
            _readyScope = await messageClient.WithHandlers(
                MessageHandler.CreateCommandHandler(
                    Roles.Coordinator,
                    Commands.Coordinator.ReportReady,
                    GetReportReady(clientAgentCount, clientPodCount, serverPodCount,cancellationToken, out var clientPodReady,
                        out var serverPodReady)));

            _logger.LogInformation("Test job {testId}: Creating server pods.", Job.TestId);
            _url = await K8SProvider.CreateServerPodsAsync(Job.TestId, asrsConnectionStrings,
                serverPodCount, Job.TestMethod, Job.ScenarioSetting.Protocol.GetFormatProtocol(),
                serverPodCount == 0 ? 0 : Job.ScenarioSetting.TotalConnectionCount / serverPodCount,Job.ScenarioSetting.Rounds[0].ClientSettings[0].Behavior,
                cancellationToken);
            _logger.LogInformation("Test job {testId}: Creating client pods.", Job.TestId);
            await K8SProvider.CreateClientPodsAsync(Job, _testStatusEntity, clientPodCount, cancellationToken);
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

        private async Task StartClientConnectionsAsync(
            IMessageClient messageClient,
            CancellationToken cancellationToken)
        {
            //dirty logic, reset group count, assume only one group
            if (Job.ScenarioSetting.GroupDefinitions.Length > 0)
            {
                var groupSize = Job.ScenarioSetting.GroupDefinitions[0].GroupSize;
                Job.ScenarioSetting.GroupDefinitions[0].GroupCount =
                    _roundTotalConnected / groupSize + (_roundTotalConnected % groupSize == 0 ? 0 : 1);
            }
            var startClientConnectionsParameters =  new StartClientConnectionsParameters
            {
                ClientLifetime = Job.ScenarioSetting.ClientLifetime,
                GroupDefinitions = Job.ScenarioSetting.GroupDefinitions,
                IsAnonymous = Job.ScenarioSetting.IsAnonymous,
                Protocol = Job.ScenarioSetting.Protocol,
                Rate = Job.ScenarioSetting.Rate / _clients.Count,
                Url = _url,
                ClientExpectServerAck = Job.ScenarioSetting.ClientExpectServerAck,
                ServerExpectClientAck = Job.ScenarioSetting.ServerExpectClientAck,
                PublishQos = Job.ScenarioSetting.PublishQos,
                SubscribeQos = Job.ScenarioSetting.SubscribeQos,
            };
            await SendAndWaitAsync(messageClient, Roles.Clients, Commands.Clients.StartClientConnections,
                startClientConnectionsParameters, _clients.Keys.ToArray(), cancellationToken);
            _currentRound.StartClientConnectionsParameters = startClientConnectionsParameters;
            _logger.LogInformation(" start  client connections acked.");
        }

        private async Task SetScenarioAsync(
            IMessageClient messageClient,
            RoundSetting round,
            CancellationToken cancellationToken)
        {
            var setScenarioParameters= new SetScenarioParameters
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
                    })};
            await SendAndWaitAsync(messageClient, Roles.Clients, Commands.Clients.SetScenario,
                setScenarioParameters, _clients.Keys.ToArray(), cancellationToken);
            _currentRound.SetScenarioParameters = setScenarioParameters;
            _logger.LogInformation(" set scenario acked.");
        }

        private async Task StartScenarioAsync(
            IMessageClient messageClient, bool save,
            CancellationToken cancellationToken)
        {
            var  startScenarioParameters =   new StartScenarioParameters()
            {
                Save = save
            };
            await SendAndWaitAsync(messageClient, Roles.Clients, Commands.Clients.StartScenario,
                startScenarioParameters, _clients.Keys.ToArray(), cancellationToken);
            _currentRound.StartScenarioParameters = startScenarioParameters;
            _logger.LogInformation(" Start scenario acked.");
        }

        private async Task StopScenarioAsync(
            IMessageClient messageClient,
            CancellationToken cancellationToken)
        {
            var stopScenarioParameters = new StopScenarioParameters();
            await SendAndWaitAsync(messageClient, Roles.Clients, Commands.Clients.StopScenario,
                stopScenarioParameters, _clients.Keys.ToArray(), cancellationToken);
            _currentRound.StopScenarioParameters = stopScenarioParameters;
            _history.Histories.Add(_currentRound);
            _logger.LogInformation(" Stop scenario acked.");
        }
        
        private async Task StopTestAsync(   
            IMessageClient messageClient,
            CancellationToken cancellationToken)
        {
            await messageClient.StopTestAsync();
            _logger.LogInformation(" Stop scenario.");
        }

        private async Task StopClientConnectionsAsync(
            IMessageClient messageClient,
            CancellationToken cancellationToken)
        {
            await SendAndWaitAsync(messageClient, Roles.Clients, Commands.Clients.StopClientConnections,
                new StopClientConnectionsParameters
                {
                    Rate = Job.ScenarioSetting.Rate / _clients.Count
                }, _clients.Keys.ToArray(), cancellationToken);
            _logger.LogInformation(" Stop client connections acked.");
        }

        private async Task SendAndWaitAsync(IMessageClient messageClient, string target, string command,
            object parameters, IReadOnlyCollection<string> senders, CancellationToken cancellationToken,
            TimeSpan? timeout = null)
        {
            try
            {
                await messageClient.SendCommandAndWaitForAcksAsync(target,
                    new CommandMessage { Command = command, Parameters = JObject.FromObject(parameters) },
                    senders, cancellationToken, timeout);
            }
            catch (Exception e)
            {
                if (e is OperationCanceledException && cancellationToken.IsCancellationRequested)
                    throw;
                var context = $"Test {Job.TestId}, phase {_phase}, command {command} failed: {e.Message}";
                Exception failure;
                if (e is TimeoutException)
                    failure = new TimeoutException(context, e);
                else
                    failure = new InvalidOperationException(context, e);
                AbortRun(failure);
                throw failure;
            }
        }

        private class directory
        {
            public   int _finishedJob = 0;
            public   int _unitTotal = 0;
            public   int _instanceTotal = 0; 
        }
    }
}