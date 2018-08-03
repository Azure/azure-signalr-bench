using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Bench.RpcSlave.Worker.Operations
{
    public interface IOperation
    {
        Task Do(WorkerToolkit tk);
    }
}
