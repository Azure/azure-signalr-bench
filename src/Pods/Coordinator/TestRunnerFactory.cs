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
        private readonly string _podName;
        private readonly string _redisConnectionString;
        private readonly ILogger<TestRunner> _logger;

        public TestRunnerFactory(
            IConfiguration configuration,
            IAksProvider aksProvider,
            IK8sProvider k8sProvider,
            SignalRProviderHolder signalRProviderHolder,
            IPerfStorage perfStorage,
            ILogger<TestRunner> logger)
        {
            _podName = configuration[PerfConstants.ConfigurationKeys.PodNameStringKey];
            _redisConnectionString = configuration[PerfConstants.ConfigurationKeys.RedisConnectionStringKey];
            AksProvider = aksProvider;
            K8sProvider = k8sProvider;
            SignalRProviderHolder = signalRProviderHolder;
            PerfStorage = perfStorage;
            _logger = logger;
        }

        public IAksProvider AksProvider { get; }

        public IK8sProvider K8sProvider { get; }

        public IPerfStorage PerfStorage { get; }

        public SignalRProviderHolder SignalRProviderHolder { get; }

        public TestRunner Create(
            TestJob job,
            string defaultLocation) =>
            new TestRunner(
                job,
                _podName,
                _redisConnectionString,
                AksProvider,
                K8sProvider,
                SignalRProviderHolder,
                PerfStorage,
                defaultLocation,
                _logger);
    }
}
