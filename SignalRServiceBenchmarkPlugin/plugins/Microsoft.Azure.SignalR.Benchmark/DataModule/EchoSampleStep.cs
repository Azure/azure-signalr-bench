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
            Parameters.TryGetValue(TotalKey, out object total);
            return (int)(long)total;
        }

        public int GetIdleConnetion()
        {
            Parameters.TryGetValue(IdleKey, out object idle);
            return (int)(long)idle;
        }
    }
}
