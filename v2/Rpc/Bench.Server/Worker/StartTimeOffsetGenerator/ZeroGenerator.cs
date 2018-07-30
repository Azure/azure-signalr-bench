using System;
using System.Collections.Generic;
using System.Text;

namespace Bench.RpcSlave.Worker.StartTimeOffsetGenerator
{
    class ZeroGenerator : IStartTimeOffsetGenerator
    {
        public TimeSpan Delay(TimeSpan duration)
        {
            var delay = TimeSpan.FromMilliseconds(0);
            return delay;
        }

    }
}
