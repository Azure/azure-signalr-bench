using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Bench.Common;

namespace Bench.RpcSlave.Worker.Operations
{
    abstract class BaseSignalrOp : BaseOp
    {
        public abstract void Setup();
        public abstract void SetCallbacks();
        public abstract Task StartSendMsg();

    }
}