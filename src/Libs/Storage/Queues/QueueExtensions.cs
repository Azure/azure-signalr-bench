// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.SignalRBench.Storage
{
    public static class QueueExtensions
    {
        public async static IAsyncEnumerable<QueueMessage<T>> Consume<T>(
            this IQueue<T> queue,
            TimeSpan? visibilityTimeout = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            while (true)
            {
                var message = await queue.ReceiveAsync(visibilityTimeout , cancellationToken);

                if (message != null)
                {
                    yield return message;
                }
                else
                {
                    try
                    {
                        await Task.Delay(5000, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        yield break;
                    }
                }
            }
        }
    }
}
