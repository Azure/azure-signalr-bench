// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using k8s;

namespace Azure.SignalRBench.Coordinator
{
    public class K8sProvider
    {
        private Kubernetes? _k8s;

        public Kubernetes K8s => _k8s ?? throw new InvalidOperationException();

        public void Initialize(string config)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(config));
            _k8s = new Kubernetes(KubernetesClientConfiguration.BuildConfigFromConfigFile(stream));
        }

        public Task<string> CreateServerPodsAsync(string testId, int nodePoolIndex, string[] asrsConnectionStrings, CancellationToken cancellationToken)
        {
            // todo: create service
            // todo: create server deployment
            return Task.FromResult(string.Empty);
        }

        public Task CreateClientPodsAsync(string testId, int nodePoolIndex, string url, CancellationToken cancellationToken)
        {
            // todo: create client deployment
            return Task.CompletedTask;
        }

        public Task DeleteClientPodsAsync(string testId, int nodePoolIndex)
        {
            // todo: delete client deployment
            return Task.CompletedTask;
        }

        public Task DeleteServerPodsAsync(string testId, int nodePoolIndex)
        {
            // todo: delete service
            // todo: delete server deployment
            return Task.CompletedTask;
        }
    }
}
