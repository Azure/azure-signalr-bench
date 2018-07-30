using Bench.RpcSlave.Worker.Savers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bench.RpcSlave.Worker.StartTimeOffsetGenerator
{
    class RandomGenerator : IStartTimeOffsetGenerator
    {
        Random _rand = new Random(DateTime.Now.Millisecond);
        private LocalFileSaver localFileSaver;

        public RandomGenerator(LocalFileSaver localFileSaver)
        {
            this.localFileSaver = localFileSaver;
        }

        public TimeSpan Delay(TimeSpan duration)
        {
            var randonDelay = TimeSpan.FromMilliseconds(_rand.Next((int)(duration.TotalMilliseconds - 1)) + 1);
            return randonDelay;
        }

    }
}
