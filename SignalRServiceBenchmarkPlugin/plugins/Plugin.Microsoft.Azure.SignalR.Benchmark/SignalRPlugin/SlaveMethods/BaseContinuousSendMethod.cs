using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public class BaseContinuousSendMethod
    {
        protected async Task ContinuousSend<T, TKey, TValue>(
        T connection, IDictionary<TKey, TValue> data, 
        Func<T, IDictionary<TKey, TValue>, Task> f, 
        TimeSpan duration, TimeSpan interval)
        {
            // Random delay in [1, interval) ms
            var rand = new Random(DateTime.Now.Millisecond);
            var randomDelay = TimeSpan.FromMilliseconds(rand.Next((int)(duration.TotalMilliseconds - 1)) + 1);
            await Task.Delay(randomDelay);

            // Send message continuously
            using (var cts = new CancellationTokenSource(duration))
            {
                while (!cts.IsCancellationRequested)
                {
                    await f(connection, data);
                }
            }
        }
    }
}
