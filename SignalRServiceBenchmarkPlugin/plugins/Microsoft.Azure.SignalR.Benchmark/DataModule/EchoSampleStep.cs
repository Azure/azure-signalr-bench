using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.SignalR.Benchmark.DataModule
{
    public class EchoSampleStep: SampleStep
    {
        private readonly string TotalKey = "Total";
        private readonly string IdleKey = "Idle";

        public int GetTotalConnetion()
        {
            IntegerDictionary.TryGetValue(TotalKey, out int total);
            return total;
        }

        public int GetIdleConnetion()
        {
            IntegerDictionary.TryGetValue(IdleKey, out int idle);
            return idle;
        }
    }
}
