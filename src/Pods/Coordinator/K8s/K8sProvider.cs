// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;

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
    }
}
