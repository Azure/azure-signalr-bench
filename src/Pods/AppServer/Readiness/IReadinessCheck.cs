// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace Azure.SignalRBench.AppServer.Readiness
{
    /// <summary>
    /// A condition that must hold before the server reports Ready to the coordinator.
    /// Checks run after the web server is listening; a server without checks is ready once it listens.
    /// </summary>
    public interface IReadinessCheck
    {
        string Name { get; }

        Task WaitUntilReadyAsync(CancellationToken cancellationToken);
    }
}
