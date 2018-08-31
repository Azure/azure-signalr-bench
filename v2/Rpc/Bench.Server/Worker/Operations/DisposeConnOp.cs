using Bench.Common.Config;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Bench.RpcSlave.Worker.Operations
{
    class DisposeConnOp: BaseOp, IOperation
    {
        private WorkerToolkit _tk;
        public async Task Do(WorkerToolkit tk)
        {
            _tk = tk;
            _tk.State = Common.Stat.Types.State.HubconnDisposing;
            await DisposeAsync(tk.Connections);
            _tk.State = Common.Stat.Types.State.HubconnDisposed;
            _tk.Init.Clear();
        }

        private async Task DisposeAsync(List<HubConnection> connections)
        {
            var tasks = new List<Task>(connections.Count);
            foreach (var conn in connections)
            {
                tasks.Add(conn.DisposeAsync());
            }
            await Task.WhenAll(tasks);
        }
    }
}
