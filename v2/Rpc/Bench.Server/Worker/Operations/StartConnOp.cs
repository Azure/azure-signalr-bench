using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Bench.Common;
using Bench.Common.Config;
using Bench.RpcSlave.Worker.Rest;
using Microsoft.AspNetCore.SignalR.Client;

namespace Bench.RpcSlave.Worker.Operations
{
    class StartConnOp : BaseOp, IOperation
    {
        private WorkerToolkit _tk;
        public async Task Do(WorkerToolkit tk)
        {
            _tk = tk;
            await Start(tk.Connections);
        }

        private async Task Start(List<HubConnection> connections)
        {
            Util.Log($"start connections");
            _tk.State = Stat.Types.State.HubconnConnecting;

            var swConn = new Stopwatch();
            swConn.Start();

            for (var i = 0; i < connections.Count; i++)
            {
                _tk.ConnectionIds.Add("");
            }

            Util.Log($"concurrent conn: {_tk.JobConfig.ConcurrentConnections} conn count: {connections.Count}");
            var left = connections.Count;
            var nextBatch = _tk.JobConfig.ConcurrentConnections;
            if (_tk.JobConfig.ConcurrentConnections > connections.Count)
            {
                Util.Log("Warning: concurrent connection > connections");
                nextBatch = connections.Count;
            }

            if (nextBatch <= left)
            {
                var tasks = new List<Task>(connections.Count);
                var i = 0;
                do
                {
                    for (var j = 0; j < nextBatch; j++)
                    {
                        var index = i + j;
                        tasks.Add(Task.Run(async() =>
                        {
                            var result = await ConnectionUtils.StartConnection(_tk, index);
                            if (result)
                            {
                                await GetConnectionId(connections[index], index);
                            }
                        }));
                    }

                    await Task.Delay(TimeSpan.FromSeconds(1));
                    i += nextBatch;
                    left = left - nextBatch;
                    if (left < nextBatch)
                    {
                        nextBatch = left;
                    }
                } while (left > 0);
                await Task.WhenAll(tasks);
                // Util.LogList("conn ids", _tk.ConnectionIds);
            }

            // _tk.Counters.UpdateConnectionSuccess(((ulong) connections.Count));
            swConn.Stop();
            Util.Log($"connection time: {swConn.Elapsed.TotalSeconds} s");

            _tk.State = Stat.Types.State.HubconnConnected;
        }

        private async Task GetConnectionId(HubConnection connection, int index)
        {
            connection.On("connectionId", (string connectionId) => _tk.ConnectionIds[index] = connectionId);
            await connection.SendAsync("connectionId");
        }
    }
}