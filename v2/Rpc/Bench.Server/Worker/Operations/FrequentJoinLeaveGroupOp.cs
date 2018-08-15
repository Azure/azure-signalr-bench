using System;
using System.Threading.Tasks;
using Bench.Common;

namespace Bench.RpcSlave.Worker.Operations
{
    class FrequentJoinLeaveGroupOp : JoinLeaveGroupOp, IOperation
    {
    //     private WorkerToolkit _tk;

    //     public async Task Do(WorkerToolkit tk)
    //     {
    //         var debug = Environment.GetEnvironmentVariable("debug") == "debug" ? true : false;

    //         var waitTime = 5 * 1000;
    //         if (!debug) Console.WriteLine($"wait time: {waitTime / 1000}s");
    //         if (!debug) await Task.Delay(waitTime);

    //         _tk = tk;
    //         _tk.State = Stat.Types.State.SendReady;

    //         // setup
    //         Setup();
    //         if (!debug) await Task.Delay(5000);

    //         _tk.State = Stat.Types.State.SendRunning;
    //         if (!debug) await Task.Delay(5000);

    //         // send message
    //         await JoinLeaveGroup();

    //         _tk.State = Stat.Types.State.SendComplete;
    //         Util.Log($"Sending Complete");
    //     }

    // }

    // protected async Task Send()
    // {

    // }
    }

}