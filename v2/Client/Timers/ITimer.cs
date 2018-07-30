using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
namespace Client.Timers
{
    public interface ITimer
    {
        void Setup();
        void Start();
        void Stop();
        void Change(int ms);
        
    }
}
