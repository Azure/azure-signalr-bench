using Client.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace Client.Workers.OperationsNs
{
    class OperationFactory
    {
        static public IOperation CreateOperation(string scenario, BaseTool pkg)
        {
            switch(scenario)
            {
                case "echo":
                    return new EchoOp(pkg);
                default:
                    return new EchoOp(pkg);
            }
        }

    }
}
