// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

using Azure.SignalRBench.Common;

namespace Azure.SignalRBench.Client
{
    public static class ClientAgentExtention
    {
        public static async Task ContinueSend(this IClientAgent client, string payload, TimeSpan interval, TimeSpan duration, Func<string, Task> func, CancellationToken cancellationToken)
        {
            await Task.Delay(StaticRandom.Next((int)interval.TotalMilliseconds));
            var cts = new CancellationTokenSource(duration);
            using var linkSource = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);
            while (!linkSource.IsCancellationRequested)
            {
                await func(payload);
                client.Context.IncreaseMessageSent();
                await Task.Delay(interval);
            }
        }

        public static async Task ContinueSend(this IClientAgent client, string group, string payload, TimeSpan interval, TimeSpan duration, Func<string, string, Task> func, CancellationToken cancellationToken)
        {
            await Task.Delay(StaticRandom.Next((int)interval.TotalMilliseconds));
            var cts = new CancellationTokenSource(duration);
            using var linkSource = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);
            while (!linkSource.IsCancellationRequested)
            {
                await func(group, payload);
                client.Context.IncreaseMessageSent();
                await Task.Delay(interval);
            }
        }
    }
}
