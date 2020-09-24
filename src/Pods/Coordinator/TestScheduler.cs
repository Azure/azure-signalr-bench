// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

using Azure.SignalRBench.Common;
using Azure.SignalRBench.Storage;
using Microsoft.Extensions.Logging;

namespace Azure.SignalRBench.Coordinator
{
    public class TestScheduler
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly ILogger<TestScheduler> _logger;
        private Task _task = Task.CompletedTask;

        public TestScheduler(PerfStorageProvider storageProvider, ILogger<TestScheduler> logger)
        {
            StorageProvider = storageProvider;
            _logger = logger;
        }

        public PerfStorageProvider StorageProvider { get; }

        public async Task StartAsync()
        {
            var queue = await StorageProvider.Storage.GetQueueAsync<TestJob>(Constants.QueueNames.PortalJob, true);
            // create table.
            _task = RunAsync(queue, _cts.Token);
        }

        public async Task StopAsync()
        {
            _cts.Cancel();
            try
            {
                await _task;
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task RunAsync(IQueue<TestJob> queue, CancellationToken cancellationToken)
        {
            await foreach (var message in queue.Consume(TimeSpan.FromMinutes(30), cancellationToken))
            {
                _logger.LogInformation("Recieve test job: {testId}.", message.Value.TestId);
                // todo: create table record.
                using var cts = new CancellationTokenSource();
                using var link = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);
                // do the job
                var jobTask = RunJobAsync(message.Value, link.Token);
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
                // todo: if (manual cancel)
                // {
                //    cts.Cancel();
                //    return;
                // }
                await queue.UpdateAsync(message, TimeSpan.FromMinutes(30), cancellationToken);
            }
        }

        private async Task RunJobAsync(TestJob job, CancellationToken cancellationToken)
        {
            if (job.ServiceSetting.Length == 0)
            {
                _logger.LogWarning("Test job {testId}: No service configuration.", job.TestId);
                return;
            }
            foreach (var ss in job.ServiceSetting)
            {
                if (ss.AsrsConnectionString == null)
                {
                    _logger.LogInformation("Test job {testId}: Creating SignalR service instance.", job.TestId);
                    // todo: create instance.
                    _logger.LogInformation("Test job {testId}: SignalR service instance created.", job.TestId);
                    _logger.LogInformation("Test job {testId}: Retrieving SignalR service connection string.", job.TestId);
                    // todo: get connection string.
                    _logger.LogInformation("Test job {testId}: SignalR service connection string retrieved.", job.TestId);
                }
            }
            var clientAgentCount = job.ScenarioSetting.TotalConnectionCount;
            var clientPodCount = (int)Math.Ceiling(clientAgentCount / 3000d);
            _logger.LogInformation("Test job {testId}: Client pods count: {count}.", job.TestId, clientPodCount);
            var serverPodCount = job.ServerSetting.ServerCount;
            _logger.LogInformation("Test job {testId}: Server pods count: {count}.", job.TestId, serverPodCount);
            var nodeCount = clientAgentCount + serverPodCount;
            _logger.LogInformation("Test job {testId}: Node count: {count}.", job.TestId, nodeCount);
            // todo: ensure node pool.
            try
            {
                // todo: creating client pods.
                _logger.LogInformation("Test job {testId}: Creating client pods.", job.TestId);
                // todo: creating server pods.
                _logger.LogInformation("Test job {testId}: Creating server pods.", job.TestId);
                // todo: waiting client pods
                _logger.LogInformation("Test job {testId}: Client pods ready.", job.TestId);
                // todo: waiting server pods
                _logger.LogInformation("Test job {testId}: Server pods ready.", job.TestId);

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
                // todo: remove client deployments.
                // todo: remove server deployments.
                _logger.LogInformation("Test job {testId}: Removing client pods.", job.TestId);
                _logger.LogInformation("Test job {testId}: Removing server pods.", job.TestId);
            }
        }
    }
}
