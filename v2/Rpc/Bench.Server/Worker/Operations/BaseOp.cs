using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Bench.Common;

namespace Bench.RpcSlave.Worker.Operations
{
    class BaseOp
    {
        public BaseOp()
        {
            var opName = GetType().Name;
            Util.Log(opName.Substring(0, opName.Length - 2) + " Operation Started.");
        }

    }
}