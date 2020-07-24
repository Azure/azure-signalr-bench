// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.SignalRBench.Storage
{
    public interface IQueue<T>
    {
        Task CreateIfNotExistedAsync();

        Task<QueueMessage<T>> ReceiveAsync(TimeSpan? visibilityTimeout = null, CancellationToken cancellationToken = default);

        Task DeleteAsync(QueueMessage<T> message, CancellationToken cancellationToken = default);

        Task SendAsync(T message, CancellationToken cancellationToken = default);

        Task UpdateAsync(QueueMessage<T> message, TimeSpan visibilityTimeout, CancellationToken cancellationToken = default);
    }
}