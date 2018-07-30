using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Client.Timers
{
    class NoWaitTimer : ITimer
    {
        Timer _timer;
        public void Change(int ms)
        {

            throw new NotImplementedException();
        }

        public void Setup()
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }
}
