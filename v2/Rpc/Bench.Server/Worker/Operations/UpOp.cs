using System.Threading.Tasks;

namespace Bench.RpcSlave.Worker.Operations
{
    class UpOp : BaseOp
    {
        public async Task Do(WorkerToolkit tk)
        {
            tk.Counters.ResetCounters(withConnection: false, withGroup: false);
        }
    }

    class UpLastOneOp : UpOp { }
    class Up0Op : UpOp { }
    class Up1Op : UpOp { }
    class Up2Op : UpOp { }
    class Up5Op : UpOp { }

    class Up10Op : UpOp { }
    class Up20Op : UpOp { }
    class Up50Op : UpOp { }

    class Up100Op : UpOp { }
    class Up200Op : UpOp { }
    class Up500Op : UpOp { }

    class Up1000Op : UpOp { }
    class Up2000Op : UpOp { }
    class Up5000Op : UpOp { }

    class Up10000Op : UpOp { }
    class Up20000Op : UpOp { }
    class Up50000Op : UpOp { }

    class Up100000Op : UpOp { }
    class Up200000Op : UpOp { }
    class Up500000Op : UpOp { }

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