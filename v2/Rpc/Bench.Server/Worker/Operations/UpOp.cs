namespace Bench.RpcSlave.Worker.Operations
{
    class UpOp : BaseOp
    {
        public void Do(WorkerToolkit tk)
        {

        }
    }

    class Up1p : UpOp { }
    class Up2p : UpOp { }
    class Up5p : UpOp { }

    class Up100p : UpOp { }
    class Up200p : UpOp { }
    class Up500p : UpOp { }

    class Up1000p : UpOp { }
    class Up2000p : UpOp { }
    class Up5000p : UpOp { }
}