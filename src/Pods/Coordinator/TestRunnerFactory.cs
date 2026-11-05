// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Coordinator.Entities;
using Azure.SignalRBench.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Azure.SignalRBench.Coordinator
{
    public class TestRunnerFactory
    {
        private readonly ILogger<TestRunner> _logger;
        private readonly string _podName;
        private readonly string _redisConnectionString;
        private readonly IDictionary<string, TestRunner> _testRunners = new ConcurrentDictionary<string, TestRunner>();
        // Cancellation requests and the cleanup scan can target the same inactive run.
        private readonly SemaphoreSlim _stopLock = new SemaphoreSlim(1, 1);

        public TestRunnerFactory(
            IConfiguration configuration,
            IAksProvider aksProvider,
            IK8sProvider k8SProvider,
            SignalRProvider signalRProvider,
            IPerfStorage perfStorage,
            ILogger<TestRunner> logger)
        {
            _podName = configuration[PerfConstants.ConfigurationKeys.PodNameStringKey];
            _redisConnectionString = configuration[PerfConstants.ConfigurationKeys.RedisConnectionStringKey];
            AksProvider = aksProvider;
            K8sProvider = k8SProvider;
            SignalRProvider = signalRProvider;
            PerfStorage = perfStorage;
            _logger = logger;
        }

        public IAksProvider AksProvider { get; }

        public IK8sProvider K8sProvider { get; }

        public IPerfStorage PerfStorage { get; }

        public SignalRProvider SignalRProvider { get; }

        public TestRunner Create(
            TestJob job,
            string defaultLocation)
        {
            var runner = new TestRunner(
                job,
                _podName,
                _redisConnectionString,
                AksProvider,
                K8sProvider,
                SignalRProvider,
                PerfStorage,
                defaultLocation,
                ()=>
                {
                    _testRunners.Remove(job.TestId);
                },
                _logger);
            _testRunners.Add(job.TestId, runner);
            return runner;
        }

        public async Task Stop(string testId)
        {
            if (_testRunners.TryGetValue(testId, out var runner))
            {
                await runner.StopAsync();
                _logger.LogInformation("TestRunner {testId} stop requested", testId);
            }
            else
            {
                await _stopLock.WaitAsync();
                try
                {
                    if (_testRunners.TryGetValue(testId, out runner))
                    {
                        await runner.StopAsync();
                        return;
                    }
                    var pairs = testId.Split("--");
                    var testStatusAccessor =
                        await PerfStorage.GetTableAsync<TestStatusEntity>(PerfConstants.TableNames.TestStatus);
                    var testStatusEntity = await testStatusAccessor.GetAsync(pairs[0], pairs[1]);
                    if (testStatusEntity == null || testStatusEntity.JobState == TestState.Cleaned.ToString())
                        return;
                    if ((testStatusEntity.JobState == TestState.InProgress.ToString() ||
                         testStatusEntity.JobState == TestState.Longrun.ToString()) &&
                        testStatusEntity.Check != "Failed" && testStatusEntity.Check != "Cancelled")
                    {
                        testStatusEntity.Healthy = false;
                        testStatusEntity.Check = "Cancelled";
                        testStatusEntity.Status = testStatusEntity.LongRun ? "Long run cancelled" : "Test cancelled";
                    }
                    testStatusEntity.JobState = TestState.Cleaning.ToString();
                    await testStatusAccessor.UpdateAsync(testStatusEntity);
                    try
                    {
                        _logger.LogInformation("Test job {testId}: Removing hashTable in redis.", testId);
                        // await messageClient.DeleteHashTableAsync();
                        _logger.LogInformation("Test job {testId}: Removing client pods.", testId);
                        await K8sProvider.DeleteClientPodsAsync(testId);
                        _logger.LogInformation("Test job {testId}: Removing server pods.",testId);
                        await K8sProvider.DeleteServerPodsAsync(testId,false);
                        testStatusEntity.JobState = TestState.Cleaned.ToString();
                        await testStatusAccessor.UpdateAsync(testStatusEntity);
                        _logger.LogInformation("Test job {testId}: cleaned.", testId);
                    }
                    catch (Exception e)
                    {
                        testStatusEntity.Healthy = false;
                        if (testStatusEntity.Check != "Failed" && testStatusEntity.Check != "Cancelled")
                            testStatusEntity.Check = "Failed";
                        testStatusEntity.Status += "; cleanup failed";
                        testStatusEntity.ErrorInfo += Environment.NewLine + e;
                        await testStatusAccessor.UpdateAsync(testStatusEntity);
                        throw;
                    }
                }
                finally
                {
                    _stopLock.Release();
                }
            }
        }
    }
}