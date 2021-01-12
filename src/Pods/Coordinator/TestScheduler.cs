// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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
        private readonly List<Task> _runningTasks = new List<Task>();
        private string? _defaultLocation;

        public TestScheduler(
            PerfStorageProvider storageProvider,
            TestRunnerFactory testRunnerFactory,
            ILogger<TestScheduler> logger)
        {
            StorageProvider = storageProvider;
            TestRunnerFactory = testRunnerFactory;
            _logger = logger;
        }

        public PerfStorageProvider StorageProvider { get; }


        public TestRunnerFactory TestRunnerFactory { get; }

        public string DefaultLocation => _defaultLocation ?? throw new InvalidOperationException();

        public async Task StartAsync(string defaultLocation)
        {
            _defaultLocation = defaultLocation;
            var queue = await StorageProvider.Storage.GetQueueAsync<TestJob>(PerfConstants.QueueNames.PortalJob, true);
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
            await foreach (var message in queue.Consume(TimeSpan.FromMinutes(30), cancellationToken))
            {
                _logger.LogInformation("Receive test job: {testId}.", message.Value.TestId);
                //Keep reference of task. Or the async state machine will be GC because we use taskCompleteSource to track pod ready
                _runningTasks.Add(RunOneAsync(queue, message, cancellationToken));
                _runningTasks.RemoveAll(t => t.IsCompleted);
            }
        }

        private async Task RunOneAsync(IQueue<TestJob> queue, QueueMessage<TestJob> message,
            CancellationToken cancellationToken)
        {
            // todo: create table record.
            using var cts = new CancellationTokenSource();
            using var link = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);
            // do the job
            var jobTask = RunJobAsync(message.Value, link.Token);
            // and renew visiblitiy.
            // await Renew(queue, message, jobTask, cts, cancellationToken);
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

        private Task RunJobAsync(TestJob job, CancellationToken cancellationToken)
        {
            return TestRunnerFactory.Create(job, DefaultLocation).RunAsync(cancellationToken);
        }
    }
}