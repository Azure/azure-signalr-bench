// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

using Azure.SignalRBench.Common;
using Azure.SignalRBench.Storage;

namespace Azure.SignalRBench.Coordinator
{
    public class TestScheduler
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private Task _task = Task.CompletedTask;

        public void Start(IQueue<TestJob> queue)
        {
            _task = RunAsync(queue);
        }

        public async Task StopAsync()
        {
            _cts.Cancel();
            await _task;
        }

        private async Task RunAsync(IQueue<TestJob> queue)
        {
            await foreach (var message in queue.Consume(TimeSpan.FromMinutes(30), _cts.Token))
            {
                // do the job
                // and renew visiblitiy.
                await queue.DeleteAsync(message);
            }
        }
    }
}
