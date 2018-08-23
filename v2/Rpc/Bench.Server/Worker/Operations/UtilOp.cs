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

    class ConfigMessageCountPerInterval1Op : UtilOp { }
    class ConfigMessageCountPerInterval2Op : UtilOp { }
    class ConfigMessageCountPerInterval3Op : UtilOp { }
    class ConfigMessageCountPerInterval4Op : UtilOp { }
    class ConfigMessageCountPerInterval5Op : UtilOp { }
    class ConfigMessageCountPerInterval6Op : UtilOp { }
    class ConfigMessageCountPerInterval7Op : UtilOp { }
    class ConfigMessageCountPerInterval8Op : UtilOp { }
    class ConfigMessageCountPerInterval9Op : UtilOp { }
    class ConfigMessageCountPerInterval10Op : UtilOp { }
}