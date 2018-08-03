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
        public async Task Do(WorkerToolkit tk)
        {
            _tk = tk;
            _tk.State = Common.Stat.Types.State.HubconnDisconnecting;
            await Stop(tk.Connections);
            _tk.State = Common.Stat.Types.State.HubconnDisconnected;
        }

        private async Task Stop(List<HubConnection> connections)
        {
            var tasks = new List<Task>(connections.Count);
            foreach(var conn in connections)
            {
                tasks.Add(conn.StopAsync());
            }

            await Task.WhenAll(tasks);
        }
    }
}

