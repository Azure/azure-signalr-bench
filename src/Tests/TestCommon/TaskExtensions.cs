// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.SignalRBench.Tests
{
    public static class TaskExtensions
    {
        public static async Task WithTimeout(this Task task, TimeSpan? timeout = default)
        {
            var delay = Task.Delay(timeout ?? TimeSpan.FromSeconds(5));
            if (delay == await Task.WhenAny(task, delay))
            {
                throw new TimeoutException();
            }
            await task;
        }

        public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan? timeout = default)
        {
            var delay = Task.Delay(timeout ?? TimeSpan.FromSeconds(5));
            if (delay == await Task.WhenAny(task, delay))
            {
                throw new TimeoutException();
            }
            return await task;
        }
    }
}
