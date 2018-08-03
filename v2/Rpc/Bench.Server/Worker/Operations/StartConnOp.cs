using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Bench.Common;
using Bench.Common.Config;
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

            for (var i = 0; i < _tk.Connections.Count; i++)
            {
                _tk.ConnectionIds.Add("");
            }

            Util.Log($"concurrent conn: {_tk.JobConfig.ConcurrentConnections} conn count: {connections.Count}");
            var left = connections.Count;
            var nextBatch = _tk.JobConfig.ConcurrentConnections;
            if (nextBatch <= left)
            {
                var tasks = new List<Task>(connections.Count);
                var i = 0;
                do
                {
                    for (var j = 0; j < nextBatch; j++)
                    {
                        var index = i + j;
                        tasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                await connections[index].StartAsync();
                                await GetConnectionId(connections[index], _tk.ConnectionIds, index);

                            }
                            catch (Exception ex)
                            {
                                Util.Log($"start connection exception: {ex}");
                                _tk.Counters.IncreaseConnectionError();
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
            }


            _tk.Counters.UpdateConnectionSuccess(((ulong)_tk.Connections.Count));
            swConn.Stop();
            Util.Log($"connection time: {swConn.Elapsed.TotalSeconds}");

            _tk.State = Stat.Types.State.HubconnConnected;
        }

        private async Task GetConnectionId(HubConnection connection, List<string> targetConnectionIds, int index)
        {
            connection.On("connectionId", (string connectionId) => _tk.ConnectionIds[index] = connectionId);
            await connection.SendAsync("connectionId");
        }
    }
}