// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Coordinator.Entities;
using Azure.SignalRBench.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Azure.SignalRBench.Coordinator
{
    public class TestScheduler
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly ILogger<TestScheduler> _logger;
        private readonly List<Task> _runningTasks = new List<Task>();
        private string? _defaultLocation;
        private string _queueName;

        public TestScheduler(
            IPerfStorage perfStorage,
            TestRunnerFactory testRunnerFactory,
            IConfiguration configuration,
            ILogger<TestScheduler> logger)
        {
            PerfStorage = perfStorage;
            TestRunnerFactory = testRunnerFactory;
            var hostLocation = configuration[PerfConstants.ConfigurationKeys.LocationKey];
            _queueName = PerfConstants.QueueNames.PortalJob;
            if (hostLocation != null && !hostLocation.Contains(PerfConstants.ConfigurationKeys.PlaceHolder))
            {
                _queueName = $"{_queueName}-{hostLocation}";
            }
            _logger = logger;
            _logger.LogInformation("queue name: {queueName}", _queueName);
        }

        public IPerfStorage PerfStorage { get; }


        public TestRunnerFactory TestRunnerFactory { get; }

        public string DefaultLocation => _defaultLocation ?? throw new InvalidOperationException();

        public async Task StartAsync(string defaultLocation)
        {
            _defaultLocation = defaultLocation;
            var queue = await PerfStorage.GetQueueAsync<TestJob>(_queueName, true);
            _ = RunAsync(queue, _cts.Token);
            _ = ScanAsync(_cts);
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
                if (message.Value.Cancel)
                {
                    _logger.LogInformation("Receive cancel job: {testId}.", message.Value.TestId);
                    _=StopJobAsync(message.Value.TestId);
                }
                else
                {
                    _logger.LogInformation("Receive test job: {testId}.", message.Value.TestId);
                    //Keep reference of task. Or the async state machine will be GC because we use taskCompleteSource to track pod ready
                    _runningTasks.Add(RunOneAsync(queue, message, cancellationToken));
                    _runningTasks.RemoveAll(t => t.IsCompleted);
                }
            }
        }

        private async Task ScanAsync(CancellationTokenSource cancellationTokenSource)
        {
            while (true)
            {
                await Task.Delay(60*1000, cancellationTokenSource.Token);
                try
                {
                    var table = await PerfStorage.GetTableAsync<TestStatusEntity>(PerfConstants.TableNames.TestStatus);
                    var fiveMinutesAgo = new DateTimeOffset(DateTime.UtcNow.AddMinutes(-5));
                    var cleaning = TestState.Cleaning.ToString();
                    var filter = FilterExpression.KeyEqual(nameof(TestStatusEntity.JobState), cleaning);
                    var result = await table
                        .QueryAsync(filter).ToListAsync();
                   
                    foreach (var test in result)
                    {
                        if (test.Timestamp > fiveMinutesAgo || test.QueueName != _queueName)
                        {
                            continue;
                        }
                        var testId = test.TestId;
                        try
                        {
                            _logger.LogInformation("Test {testId} is in cleaning state, stop it.", testId);
                            await TestRunnerFactory.Stop(testId);
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, "Stop test {testId} error.", testId);
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Get test status error");
                    throw;
                }
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
        
        private Task StopJobAsync(string testId)
        {
            return TestRunnerFactory.Stop(testId);
        }
    }
}