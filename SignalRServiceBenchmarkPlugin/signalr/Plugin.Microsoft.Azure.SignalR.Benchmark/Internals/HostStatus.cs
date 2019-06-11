using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.Internals
{
    class HostStatus
    {
        private readonly TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();

        public void SetStarted() => _tcs.TrySetResult(null);

        public Task StartedTask => _tcs.Task;
    }
}
