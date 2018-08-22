using System.Threading.Tasks;

namespace Bench.RpcSlave.Worker.Operations
{
    class UtilOp : BaseOp
    {
        public async Task Do(WorkerToolkit tk) { }
    }

    class RemoveExceptLastOneCallbackOp : UtilOp { }

    class ClearJoinLeaveCountersOp : UtilOp
    {
        public async Task Do(WorkerToolkit tk)
        {
            tk.Counters.ResetCounters(withConnection: false, withGroup: true);
        }
    }
}