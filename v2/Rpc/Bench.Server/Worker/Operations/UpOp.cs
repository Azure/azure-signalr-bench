namespace Bench.RpcSlave.Worker.Operations
{
    class UpOp : BaseOp
    {
        public void Do(WorkerToolkit tk)
        {

        }
    }

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
}