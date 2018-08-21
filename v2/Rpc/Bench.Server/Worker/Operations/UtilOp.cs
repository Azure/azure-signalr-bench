using System.Threading.Tasks;

namespace Bench.RpcSlave.Worker.Operations
{
    class UtilOp : BaseOp
    {
        public async Task Do(WorkerToolkit tk)
        {
        }
    }

    class RemoveExceptLastOneCallbackOp : UtilOp {}


}