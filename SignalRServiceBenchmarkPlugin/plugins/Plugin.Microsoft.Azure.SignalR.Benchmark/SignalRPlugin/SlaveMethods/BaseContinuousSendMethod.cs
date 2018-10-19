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
        TimeSpan duration, TimeSpan interval, TimeSpan delayMin, TimeSpan delayMax)
        {
            // Random delay in [delayMin, delayMax) ms
            var rand = new Random();
            var randomDelay = TimeSpan.FromMilliseconds(rand.Next((int)delayMin.TotalMilliseconds, (int)delayMax.TotalMilliseconds));
            await Task.Delay(randomDelay);

            // Send message continuously
            using (var cts = new CancellationTokenSource(duration))
            {
                while (!cts.IsCancellationRequested)
                {
                    await f(connection, data); // TODO: await may cause bad send rate
                    await Task.Delay(interval);
                }
            }
        }
    }
}
