using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Client.Workers.OperationsNs
{
    public interface IOperation
    {
        void Process();
        void Setup();
        void SaveCounters();
    }
}
