using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.SignalR.Benchmark.DataModule
{
    public class EchoSampleStep: SampleStep
    {
        private readonly string TotalKey = "Parameter.Total";
        private readonly string IdleKey = "Parameter.Idle";

        public int GetTotalConnetion()
        {
            Parameters.TryGetValue(TotalKey, out object total);
            return Convert.ToInt32(total);
        }

        public int GetIdleConnetion()
        {
            Parameters.TryGetValue(IdleKey, out object idle);
            return Convert.ToInt32(idle);
        }
    }
}
