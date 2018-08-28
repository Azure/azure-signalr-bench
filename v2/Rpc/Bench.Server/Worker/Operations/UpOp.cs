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
    class Up3Op : UpOp { }
    class Up4Op : UpOp { }
    class Up5Op : UpOp { }
    class Up6Op : UpOp { }
    class Up7Op : UpOp { }
    class Up8Op : UpOp { }
    class Up9Op : UpOp { }

    class Up10Op : UpOp { }
    class Up20Op : UpOp { }
    class Up30Op : UpOp { }
    class Up40Op : UpOp { }
    class Up50Op : UpOp { }
    class Up60Op : UpOp { }
    class Up70Op : UpOp { }
    class Up80Op : UpOp { }
    class Up90Op : UpOp { }

    class Up100Op : UpOp { }
    class Up200Op : UpOp { }
    class Up300Op : UpOp { }
    class Up400Op : UpOp { }
    class Up500Op : UpOp { }
    class Up600Op : UpOp { }
    class Up700Op : UpOp { }
    class Up800Op : UpOp { }
    class Up900Op : UpOp { }

    class Up1000Op : UpOp { }
    class Up2000Op : UpOp { }
    class Up3000Op : UpOp { }
    class Up4000Op : UpOp { }
    class Up5000Op : UpOp { }
    class Up6000Op : UpOp { }
    class Up7000Op : UpOp { }
    class Up8000Op : UpOp { }
    class Up9000Op : UpOp { }

    class Up10000Op : UpOp { }
    class Up20000Op : UpOp { }
    class Up30000Op : UpOp { }
    class Up40000Op : UpOp { }
    class Up50000Op : UpOp { }
    class Up60000Op : UpOp { }
    class Up70000Op : UpOp { }
    class Up80000Op : UpOp { }
    class Up90000Op : UpOp { }

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

}
