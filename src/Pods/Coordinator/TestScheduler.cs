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
        private string? _defaultLocation;
        private Task[] _runningTasks = Array.Empty<Task>();

        public TestScheduler(
            PerfStorageProvider storageProvider,
            IAksProvider aksProvider,
            IK8sProvider k8sProvider,
            ISignalRProvider signalRProvider,
            TestRunnerFactory testRunnerFactory,
            ILogger<TestScheduler> logger)
        {
            StorageProvider = storageProvider;
            AksProvider = aksProvider;
            K8sProvider = k8sProvider;
            SignalRProvider = signalRProvider;
            TestRunnerFactory = testRunnerFactory;
            _logger = logger;
        }

        public PerfStorageProvider StorageProvider { get; }

        public IAksProvider AksProvider { get; }

        public IK8sProvider K8sProvider { get; }

        public ISignalRProvider SignalRProvider { get; }

        public TestRunnerFactory TestRunnerFactory { get; }

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
            int poolCount = await AksProvider.GetNodePoolCountAsync(cancellationToken);
            var runningTasks = new Task[poolCount];
            Array.Fill(runningTasks, Task.CompletedTask);
            _runningTasks = runningTasks;
            await foreach (var message in queue.Consume(TimeSpan.FromMinutes(30), cancellationToken))
            {
                _logger.LogInformation("Recieve test job: {testId}.", message.Value.TestId);
                var index = Array.FindIndex(runningTasks, t => t.IsCompleted);
                runningTasks[index] = RunOneAsync(queue, message, index, cancellationToken);
                await Task.WhenAny(runningTasks);
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

        private Task RunJobAsync(TestJob job, int nodePoolIndex, CancellationToken cancellationToken) =>
            TestRunnerFactory.Create(job, nodePoolIndex, DefaultLocation).RunAsync(cancellationToken);
    }
}
