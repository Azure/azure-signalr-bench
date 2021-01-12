// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.SignalRBench.Common;
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

        public TestRunnerFactory(
            IConfiguration configuration,
            IAksProvider aksProvider,
            IK8sProvider k8sProvider,
            SignalRProvider signalRProvider,
            IPerfStorage perfStorage,
            ILogger<TestRunner> logger)
        {
            _podName = configuration[PerfConstants.ConfigurationKeys.PodNameStringKey];
            _redisConnectionString = configuration[PerfConstants.ConfigurationKeys.RedisConnectionStringKey];
            AksProvider = aksProvider;
            K8sProvider = k8sProvider;
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
            return new TestRunner(
                job,
                _podName,
                _redisConnectionString,
                AksProvider,
                K8sProvider,
                SignalRProvider,
                PerfStorage,
                defaultLocation,
                _logger);
        }
    }
}