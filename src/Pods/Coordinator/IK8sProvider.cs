// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;

namespace Azure.SignalRBench.Coordinator
{
    public interface IK8sProvider
    {
        Task CreateClientPodsAsync(string testId, TestCategory testCategory, int clientPodCount, CancellationToken cancellationToken);
        Task<string> CreateServerPodsAsync(string testId, string[] asrsConnectionStrings, int serverPodCount, TestCategory testCategory, CancellationToken cancellationToken);
        Task DeleteClientPodsAsync(string testId);
        Task DeleteServerPodsAsync(string testId, bool upstream);
        void Initialize(string config);
    }
}