using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Bench.Common;
using Bench.Common.Config;
using Bench.RpcSlave.Worker.Savers;
using Bench.RpcSlave.Worker.StartTimeOffsetGenerator;
using Microsoft.AspNetCore.SignalR.Client;
using Interlocked = System.Threading.Interlocked;

namespace Bench.RpcSlave.Worker.Operations
{
    class StartConnConstantOp : BaseOp, IOperation
    {
        private WorkerToolkit _tk;
        public async Task Do(WorkerToolkit tk)
        {
            var timer = new Timer(500);
            timer.AutoReset = true;
            timer.Elapsed += (s, e) =>
            {
                Util.Log($"cnt: {cnt}");
            };
            timer.Start();

            _tk = tk;
            await Start(tk.Connections);

        }

        private int cnt = 0;
        private ConcurrentQueue<HubConnection> connectionQueue;
        private async Task Start(List<HubConnection> connections)
        {
            // if (Environment.GetEnvironmentVariable("connect") == "true") File.WriteAllText("connect.txt", "");
            Util.Log($"start connections");
            _tk.State = Stat.Types.State.HubconnConnecting;

            var swConn = new Stopwatch();
            swConn.Start();

            _tk.ConnectionIds = Enumerable.Repeat("", connections.Count).ToList();

            Util.Log($"concurrent conn: {_tk.JobConfig.ConcurrentConnections} conn count: {connections.Count}");

            await ConcurrentConnectService(connections, StartConnect, _tk.JobConfig.ConcurrentConnections);

            swConn.Stop();
            Util.Log($"connection time: {swConn.Elapsed.TotalSeconds} s");

            _tk.State = Stat.Types.State.HubconnConnected;
        }

        public static Task ConcurrentConnectService<T>(IEnumerable<T> sourse, Func<T, Task> f, int max)
        {
            var initial = (max >> 1);
            var s = new SemaphoreSlim(initial, max);
            _ = Task.Run(async () => {
                for (int i = initial; i < max; i++)
                {
                    await Task.Delay(100);
                    s.Release();
                }
            });
            return Task.WhenAll(from item in sourse
                                select Task.Run(async () =>
                                {
                                    await s.WaitAsync();
                                    try
                                    {
                                        await f(item);
                                    }
                                    finally
                                    {
                                        s.Release();
                                    }
                                }));
        }

        private async Task StartConnect(HubConnection connection)
        {
            if (connection == null) return;
            try
            {
                Interlocked.Increment(ref cnt);
                await connection.StartAsync();
                Interlocked.Decrement(ref cnt);
            }
            catch (Exception ex)
            {
                Util.Log($"start connection exception: {ex}");
                Environment.Exit(1); //debug
                _tk.Counters.IncreaseConnectionError();
            }
        }

        private async Task GetConnectionId(HubConnection connection, List<string> targetConnectionIds, int index)
        {
            connection.On("connectionId", (string connectionId) => _tk.ConnectionIds[index] = connectionId);
            await connection.SendAsync("connectionId");
        }
    }
}