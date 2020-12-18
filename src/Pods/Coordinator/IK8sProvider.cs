// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;

namespace Azure.SignalRBench.Coordinator
{
    public interface IK8sProvider
    {
        Task CreateClientPodsAsync(string testId,TestCategory testCategory, int nodePoolIndex,int clientPodCount, CancellationToken cancellationToken);
        Task<string> CreateServerPodsAsync(string testId, int nodePoolIndex, string[] asrsConnectionStrings,int serverPodCount,bool upstream, CancellationToken cancellationToken);
        Task DeleteClientPodsAsync(string testId, int nodePoolIndex);
        Task DeleteServerPodsAsync(string testId, int nodePoolIndex,bool upstream);
        void Initialize(string config);
    }
}