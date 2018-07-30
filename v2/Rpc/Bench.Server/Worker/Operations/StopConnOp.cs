using Bench.Common.Config;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Bench.RpcSlave.Worker.Operations
{
    class StopConnOp : BaseOp, IOperation
    {
        private WorkerToolkit _tk; 
        public void Do(WorkerToolkit tk)
        {
            _tk = tk;
            _tk.State = Common.Stat.Types.State.HubconnDisconnecting;
            Stop(tk.Connections);
            _tk.State = Common.Stat.Types.State.HubconnDisconnected;

        }

        private void Stop(List<HubConnection> connections)
        {
            var tasks = new List<Task>(connections.Count);
            foreach(var conn in connections)
            {
                tasks.Add(conn.StopAsync());
            }

            Task.WhenAll(tasks).Wait();
        }
    }
}

