// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Azure.SignalRBench.Common;
using Azure.SignalRBench.Messages;
using Microsoft.Extensions.Logging;

namespace Azure.SignalRBench.Coordinator
{
    public class TestRunner
    {
        private const double MaxClientCountInPod = 3000;

        private readonly ILogger<TestRunner> _logger;

        public TestRunner(
            TestJob job,
            string podName,
            string redisConnectionString,
            int nodePoolIndex,
            AksProvider aksProvider,
            K8sProvider k8sProvider,
            SignalRProvider signalRProvider,
            string defaultLocation,
            ILogger<TestRunner> logger)
        {
            Job = job;
            PodName = podName;
            RedisConnectionString = redisConnectionString;
            NodePoolIndex = nodePoolIndex;
            AksProvider = aksProvider;
            K8sProvider = k8sProvider;
            SignalRProvider = signalRProvider;
            DefaultLocation = defaultLocation;
            _logger = logger;
        }

        public TestJob Job { get; }

        public string PodName { get; set; }

        public string RedisConnectionString { get; set; }

        public int NodePoolIndex { get; set; }

        public AksProvider AksProvider { get; }

        public K8sProvider K8sProvider { get; }

        public SignalRProvider SignalRProvider { get; }

        public string DefaultLocation { get; }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            if (Job.ServiceSetting.Length == 0)
            {
                _logger.LogWarning("Test job {testId}: No service configuration.", Job.TestId);
                return;
            }

            var clientAgentCount = Job.ScenarioSetting.TotalConnectionCount;
            var clientPodCount = (int)Math.Ceiling(clientAgentCount / MaxClientCountInPod);
            _logger.LogInformation("Test job {testId}: Client pods count: {count}.", Job.TestId, clientPodCount);
            var serverPodCount = Job.ServerSetting.ServerCount;
            _logger.LogInformation("Test job {testId}: Server pods count: {count}.", Job.TestId, serverPodCount);
            var nodeCount = clientAgentCount + serverPodCount;
            _logger.LogInformation("Test job {testId}: Node count: {count}.", Job.TestId, nodeCount);

            var asrsConnectionStringsTask = PrepairAsrsInstancesAsync(cancellationToken);
            await AksProvider.EnsureNodeCountAsync(NodePoolIndex, nodeCount, cancellationToken);
            using var messageClient = await MessageClient.ConnectAsync(RedisConnectionString, Job.TestId, PodName);
            try
            {
                await CreatePodsAsync(await asrsConnectionStringsTask, clientAgentCount, clientPodCount, serverPodCount, messageClient, cancellationToken);
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
                _logger.LogInformation("Test job {testId}: Removing client pods.", Job.TestId);
                await K8sProvider.DeleteClientPodsAsync(Job.TestId, NodePoolIndex);
                _logger.LogInformation("Test job {testId}: Removing server pods.", Job.TestId);
                await K8sProvider.DeleteServerPodsAsync(Job.TestId, NodePoolIndex);
                _logger.LogInformation("Test job {testId}: Removing service instances.", Job.TestId);
                await Task.WhenAll(
                    from ss in Job.ServiceSetting
                    where ss.AsrsConnectionString == null
                    group ss by ss.Location ?? DefaultLocation into g
                    select SignalRProvider.DeleteResourceGroupAsync(Job.TestId, g.Key));
            }
        }

        private async Task CreatePodsAsync(string[] asrsConnectionStrings, int clientAgentCount, int clientPodCount, int serverPodCount, MessageClient messageClient, CancellationToken cancellationToken)
        {
            var clients = new Dictionary<string, SetClientRangeParameters>();
            var servers = new List<string>();
            await messageClient.WithHandlers(
                MessageHandler.CreateCommandHandler(
                    Roles.Coordinator,
                    Commands.Coordinator.ReportReady,
                    GetReportReady(clientAgentCount, clientPodCount, serverPodCount, clients, servers, out var clientPodReady, out var serverPodReady)));

            _logger.LogInformation("Test job {testId}: Creating server pods.", Job.TestId);
            await K8sProvider.CreateServerPodsAsync(Job.TestId, NodePoolIndex, asrsConnectionStrings, cancellationToken);
            // todo: get url for negotiate.
            string url = string.Empty;
            _logger.LogInformation("Test job {testId}: Creating client pods.", Job.TestId);
            await K8sProvider.CreateClientPodsAsync(Job.TestId, NodePoolIndex, url, cancellationToken);

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

            var clientCompleteTask = await messageClient.WhenAllAck(
                clients.Keys,
                Commands.Clients.SetClientRange,
                m => m.IsCompleted,
                cancellationToken);
            await Task.WhenAll(clients.Select(pair => messageClient.SetClientRangeAsync(pair.Key, pair.Value)));
            await clientCompleteTask;
        }

        private async Task<string[]> PrepairAsrsInstancesAsync(CancellationToken cancellationToken)
        {
            var asrsConnectionStrings = new string[Job.ServiceSetting.Length];
            for (int i = 0; i < Job.ServiceSetting.Length; i++)
            {
                var ss = Job.ServiceSetting[i];
                if (ss.AsrsConnectionString == null)
                {
                    asrsConnectionStrings[i] = await CreateAsrsAsync(ss, Job.TestId + i.ToString(), cancellationToken);
                }
                else
                {
                    asrsConnectionStrings[i] = ss.AsrsConnectionString;
                }
            }

            return asrsConnectionStrings;
        }

        private async Task<string> CreateAsrsAsync(ServiceSetting ss, string name, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Test job {testId}: Creating SignalR service instance.", Job.TestId);
            await SignalRProvider.CreateInstanceAsync(
                Job.TestId,
                name,
                ss.Location ?? DefaultLocation,
                ss.Tier ?? "Standard",
                ss.Size ?? 1,
                Job.TestMethod.GetServiceMode(),
                cancellationToken);
            _logger.LogInformation("Test job {testId}: SignalR service instance created.", Job.TestId);
            _logger.LogInformation("Test job {testId}: Retrieving SignalR service connection string.", Job.TestId);
            var result = await SignalRProvider.CreateKeyAsync(Job.TestId, name, ss.Location ?? DefaultLocation, cancellationToken);
            _logger.LogInformation("Test job {testId}: SignalR service connection string retrieved.", Job.TestId);
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
