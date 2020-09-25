// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Azure.SignalRBench.Common;
using Azure.SignalRBench.Messages;
using Azure.SignalRBench.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Azure.SignalRBench.Coordinator
{
    public class TestScheduler
    {
        private const double MaxClientCountInPod = 3000;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly string _podName;
        private readonly string _redisConnectionString;
        private readonly ILogger<TestScheduler> _logger;
        private string? _defaultLocation;
        private Task[] _runningTasks = Array.Empty<Task>();

        public TestScheduler(
            IConfiguration configuration,
            PerfStorageProvider storageProvider,
            AksProvider aksProvider,
            K8sProvider k8sProvider,
            SignalRProvider signalRProvider,
            ILogger<TestScheduler> logger)
        {
            _podName = configuration[Constants.ConfigurationKeys.PodNameStringKey];
            _redisConnectionString = configuration[Constants.ConfigurationKeys.RedisConnectionStringKey];
            StorageProvider = storageProvider;
            AksProvider = aksProvider;
            K8sProvider = k8sProvider;
            SignalRProvider = signalRProvider;
            _logger = logger;
        }

        public PerfStorageProvider StorageProvider { get; }

        public AksProvider AksProvider { get; }

        public K8sProvider K8sProvider { get; }

        public SignalRProvider SignalRProvider { get; }

        public string DefaultLocation => _defaultLocation ?? throw new InvalidOperationException();

        public async Task StartAsync(string defaultLocation)
        {
            _defaultLocation = defaultLocation;
            var queue = await StorageProvider.Storage.GetQueueAsync<TestJob>(Constants.QueueNames.PortalJob, true);
            // create table.
            _ = RunAsync(queue, _cts.Token);
        }

        public async Task StopAsync()
        {
            _cts.Cancel();
            try
            {
                await Task.WhenAll(_runningTasks);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task RunAsync(IQueue<TestJob> queue, CancellationToken cancellationToken)
        {
            int poolCount = await AksProvider.GetNodePoolCountAsync();
            _runningTasks = new Task[poolCount];
            Array.Fill(_runningTasks, Task.CompletedTask);
            await foreach (var message in queue.Consume(TimeSpan.FromMinutes(30), cancellationToken))
            {
                _logger.LogInformation("Recieve test job: {testId}.", message.Value.TestId);
                var index = Array.FindIndex(_runningTasks, t => t.IsCompleted);
                _runningTasks[index] = RunOneAsync(queue, message, index, cancellationToken);
                await Task.WhenAny(_runningTasks);
            }
        }

        private async Task RunOneAsync(IQueue<TestJob> queue, QueueMessage<TestJob> message, int nodePoolIndex, CancellationToken cancellationToken)
        {
            // todo: create table record.
            using var cts = new CancellationTokenSource();
            using var link = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);
            // do the job
            var jobTask = RunJobAsync(message.Value, nodePoolIndex, link.Token);
            // and renew visiblitiy.
            await Renew(queue, message, jobTask, cts, cancellationToken);
            await queue.DeleteAsync(message);
            try
            {
                await jobTask;
                _logger.LogInformation("Test job {testId} completed.", message.Value.TestId);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Test job {testId} cancelled.", message.Value.TestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Test job {testId} stopped by unknown error.", message.Value.TestId);
            }
        }

        private async Task Renew(IQueue<TestJob> queue, QueueMessage<TestJob> message, Task jobTask, CancellationTokenSource cts, CancellationToken cancellationToken)
        {
            while (true)
            {
                var task = await Task.WhenAny(jobTask, Task.Delay(TimeSpan.FromMinutes(1), cancellationToken));
                if (jobTask == task)
                {
                    break;
                }
                if (task.IsCanceled)
                {
                    return;
                }
                // todo: check manual cancel
                // {
                //    cts.Cancel();
                //    return;
                // }
                await queue.UpdateAsync(message, TimeSpan.FromMinutes(30), cancellationToken);
            }
        }

        private async Task RunJobAsync(TestJob job, int nodePoolIndex, CancellationToken cancellationToken)
        {
            if (job.ServiceSetting.Length == 0)
            {
                _logger.LogWarning("Test job {testId}: No service configuration.", job.TestId);
                return;
            }

            var clientAgentCount = job.ScenarioSetting.TotalConnectionCount;
            var clientPodCount = (int)Math.Ceiling(clientAgentCount / MaxClientCountInPod);
            _logger.LogInformation("Test job {testId}: Client pods count: {count}.", job.TestId, clientPodCount);
            var serverPodCount = job.ServerSetting.ServerCount;
            _logger.LogInformation("Test job {testId}: Server pods count: {count}.", job.TestId, serverPodCount);
            var nodeCount = clientAgentCount + serverPodCount;
            _logger.LogInformation("Test job {testId}: Node count: {count}.", job.TestId, nodeCount);

            var asrsConnectionStringsTask = PrepairAsrsInstancesAsync(job, cancellationToken);
            await AksProvider.EnsureNodeCountAsync(nodePoolIndex, nodeCount, cancellationToken);
            using var messageClient = await MessageClient.ConnectAsync(_redisConnectionString, job.TestId, _podName);
            try
            {
                await CreatePodsAsync(job, nodePoolIndex, await asrsConnectionStringsTask, clientAgentCount, clientPodCount, serverPodCount, messageClient, cancellationToken);
                // todo: clients connect.
                // todo: set scenario.
                // todo: round 1,2,3...
                // {
                // todo: start
                // todo: stop
                // }
                // todo: client disconnect.
            }
            finally
            {
                _logger.LogInformation("Test job {testId}: Removing client pods.", job.TestId);
                await K8sProvider.DeleteClientPodsAsync(job.TestId, nodePoolIndex);
                _logger.LogInformation("Test job {testId}: Removing server pods.", job.TestId);
                await K8sProvider.DeleteServerPodsAsync(job.TestId, nodePoolIndex);
                _logger.LogInformation("Test job {testId}: Removing service instances.", job.TestId);
                await Task.WhenAll(
                    from ss in job.ServiceSetting
                    where ss.AsrsConnectionString == null
                    group ss by ss.Location ?? DefaultLocation into g
                    select SignalRProvider.DeleteResourceGroupAsync(job.TestId, g.Key));
            }
        }

        private async Task CreatePodsAsync(TestJob job, int nodePoolIndex, string[] asrsConnectionStrings, int clientAgentCount, int clientPodCount, int serverPodCount, MessageClient messageClient, CancellationToken cancellationToken)
        {
            var clients = new Dictionary<string, SetClientRangeParameters>();
            var servers = new List<string>();
            await messageClient.WithHandlers(
                MessageHandler.CreateCommandHandler(
                    Roles.Coordinator,
                    Commands.Coordinator.ReportReady,
                    GetReportReady(clientAgentCount, clientPodCount, serverPodCount, clients, servers, out var clientPodReady, out var serverPodReady)));

            _logger.LogInformation("Test job {testId}: Creating server pods.", job.TestId);
            await K8sProvider.CreateServerPodsAsync(job.TestId, nodePoolIndex, asrsConnectionStrings, cancellationToken);
            // todo: get url for negotiate.
            string url = string.Empty;
            _logger.LogInformation("Test job {testId}: Creating client pods.", job.TestId);
            await K8sProvider.CreateClientPodsAsync(job.TestId, nodePoolIndex, url, cancellationToken);

            await Task.WhenAll(
                Task.Run(async () =>
                {
                    await clientPodReady;
                    _logger.LogInformation("Test job {testId}: Client pods ready.", job.TestId);
                }),
                Task.Run(async () =>
                {
                    await serverPodReady;
                    _logger.LogInformation("Test job {testId}: Server pods ready.", job.TestId);
                }));

            var clientCompleteTask = await messageClient.WhenAllAck(
                clients.Keys,
                Commands.Clients.SetClientRange,
                m => m.IsCompleted && clients.ContainsKey(m.Sender),
                cancellationToken);

            foreach (var pair in clients)
            {
                await messageClient.SetClientRangeAsync(pair.Key, pair.Value);
            }

            await clientCompleteTask;
        }

        private async Task<string[]> PrepairAsrsInstancesAsync(TestJob job, CancellationToken cancellationToken)
        {
            var asrsConnectionStrings = new string[job.ServiceSetting.Length];
            for (int i = 0; i < job.ServiceSetting.Length; i++)
            {
                var ss = job.ServiceSetting[i];
                if (ss.AsrsConnectionString == null)
                {
                    asrsConnectionStrings[i] = await CreateAsrsAsync(job, ss, job.TestId + i.ToString(), cancellationToken);
                }
                else
                {
                    asrsConnectionStrings[i] = ss.AsrsConnectionString;
                }
            }

            return asrsConnectionStrings;
        }

        private async Task<string> CreateAsrsAsync(TestJob job, ServiceSetting ss, string name, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Test job {testId}: Creating SignalR service instance.", job.TestId);
            await SignalRProvider.CreateInstanceAsync(
                job.TestId,
                name,
                ss.Location ?? DefaultLocation,
                ss.Tier ?? "Standard",
                ss.Size ?? 1,
                job.TestMethod.GetServiceMode(),
                cancellationToken);
            _logger.LogInformation("Test job {testId}: SignalR service instance created.", job.TestId);
            _logger.LogInformation("Test job {testId}: Retrieving SignalR service connection string.", job.TestId);
            var result = await SignalRProvider.CreateInstanceKeyAsync(job.TestId, name, ss.Location ?? DefaultLocation, cancellationToken);
            _logger.LogInformation("Test job {testId}: SignalR service connection string retrieved.", job.TestId);
            return result;
        }

        private Func<CommandMessage, Task> GetReportReady(
            int clientAgentCount,
            int clientPodCount,
            int serverPodCount,
            Dictionary<string, SetClientRangeParameters> clientRangeDictionary,
            List<string> serverPods,
            out Task clientPodsReady,
            out Task serverPodsReady)
        {
            int clientReadyCount = 0;
            int serverReadyCount = 0;
            var countPerPod = clientAgentCount / clientPodCount;
            var clientPodsReadyTcs = new TaskCompletionSource<object?>();
            var serverPodsReadyTcs = new TaskCompletionSource<object?>();
            clientPodsReady = clientPodsReadyTcs.Task;
            serverPodsReady = serverPodsReadyTcs.Task;
            return m =>
            {
                var p = m.Parameters?.ToObject<ReportReadyParameters>();
                if (p == null)
                {
                    // todo: log.
                    return Task.CompletedTask;
                }
                if (p.Role == Roles.Clients)
                {
                    clientReadyCount++;
                    if (clientReadyCount < clientAgentCount)
                    {
                        clientRangeDictionary[m.Sender] =
                            new SetClientRangeParameters
                            {
                                StartId = (clientReadyCount - 1) * countPerPod,
                                Count = clientPodCount,
                            };
                    }
                    else if (clientReadyCount == clientAgentCount)
                    {
                        clientRangeDictionary[m.Sender] =
                            new SetClientRangeParameters
                            {
                                StartId = (clientReadyCount - 1) * countPerPod,
                                Count = clientAgentCount - (clientReadyCount - 1) * countPerPod,
                            };
                        clientPodsReadyTcs.TrySetResult(null);
                    }
                    else
                    {
                        // todo: log
                        clientRangeDictionary[m.Sender] =
                            new SetClientRangeParameters
                            {
                                StartId = 0,
                                Count = 0,
                            };
                    }
                }
                else if (p.Role == Roles.AppServers)
                {
                    serverReadyCount++;
                    if (serverReadyCount == serverPodCount)
                    {
                        serverPodsReadyTcs.TrySetResult(null);
                    }
                    serverPods.Add(m.Sender);
                }
                else
                {
                    // todo: log.
                }
                return Task.CompletedTask;
            };
        }
    }
}
