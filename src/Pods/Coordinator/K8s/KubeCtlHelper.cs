﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using k8s;
using System.IO;
using System.Text;

namespace Coordinator
{
    internal class KubeCtlHelper
    {
        private readonly Kubernetes _kubernetes;

        public KubeCtlHelper()
        {
            _kubernetes = GetKubeClient();
        }
        public Kubernetes GetKubeClient()
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(PerfConfig.KubeConfig)))
            {
                var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(stream);
                return new Kubernetes(config);
            };
        }
    }
}