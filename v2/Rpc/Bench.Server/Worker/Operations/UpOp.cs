using System.Threading.Tasks;

namespace Bench.RpcSlave.Worker.Operations
{
    class UpOp : BaseOp
    {
        public Task Do(WorkerToolkit tk)
        {
            tk.Counters.ResetCounters(withConnection: false, withGroup: false);
            return Task.CompletedTask;
        }
    }

    class UpLastOneOp : UpOp { }

    // per group
    class UpPerGroup1Op : UpOp { }
    class UpPerGroup2Op : UpOp { }
    class UpPerGroup5Op : UpOp { }
    class UpPerGroup10Op : UpOp { }
    class UpPerGroup20Op : UpOp { }
    class UpPerGroup50Op : UpOp { }

    // increase sending conn for join/leave group
    class UpJoinLeavePerGroup1Op : UpOp { }
    class UpJoinLeavePerGroup2Op : UpOp { }
    class UpJoinLeavePerGroup5Op : UpOp { }
    class UpJoinLeavePerGroup10Op : UpOp { }
    class UpJoinLeavePerGroup20Op : UpOp { }
    class UpJoinLeavePerGroup50Op : UpOp { }
    class UpJoinLeavePerGroup100Op : UpOp { }
    class UpJoinLeavePerGroup200Op : UpOp { }
    class UpJoinLeavePerGroup500Op : UpOp { }
    class UpJoinLeavePerGroup1000Op : UpOp { }
    class UpJoinLeavePerGroup2000Op : UpOp { }
    class UpJoinLeavePerGroup5000Op : UpOp { }
    class UpJoinLeavePerGroup10000Op : UpOp { }
    class UpJoinLeavePerGroup20000Op : UpOp { }
    class UpJoinLeavePerGroup50000Op : UpOp { }
    class UpJoinLeavePerGroup100000Op : UpOp { }
    class UpJoinLeavePerGroup200000Op : UpOp { }
    class UpJoinLeavePerGroup500000Op : UpOp { }

}
