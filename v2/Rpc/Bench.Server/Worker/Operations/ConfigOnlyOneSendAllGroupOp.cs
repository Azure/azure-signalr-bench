using System.Threading.Tasks;

namespace Bench.RpcSlave.Worker.Operations
{
    class ConfigOnlyOneSendAllGroupOp : BaseOp
    {
        public Task Do(WorkerToolkit tk)
        {
            return Task.CompletedTask;
        }
    }

}