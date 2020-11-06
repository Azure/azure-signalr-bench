// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace Azure.SignalRBench.Coordinator
{
    public interface IK8sProvider
    {
        Task CreateClientPodsAsync(string testId, int nodePoolIndex, string url,int clientPodCount, CancellationToken cancellationToken);
        Task<string> CreateServerPodsAsync(string testId, int nodePoolIndex, string[] asrsConnectionStrings,int serverPodCount, CancellationToken cancellationToken);
        Task DeleteClientPodsAsync(string testId, int nodePoolIndex);
        Task DeleteServerPodsAsync(string testId, int nodePoolIndex);
        void Initialize(string config);
    }
}