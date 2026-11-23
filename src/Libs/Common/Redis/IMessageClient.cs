// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.SignalRBench.Messages
{
    public interface IMessageClient
    {
        Task AckAsync(CommandMessage message, AckStatus status, string? error = null, double? progress = null);
        Task<IDisposable> WithHandlers(params MessageHandler[] handlers);
        Task SendCommandAsync(string target, CommandMessage message);
        Task SendCommandAndWaitForAcksAsync(string target, CommandMessage message,
            IReadOnlyCollection<string> expectedSenders, CancellationToken cancellationToken, TimeSpan? timeout = null);
    }
}