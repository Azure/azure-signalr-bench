using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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

        private int beg = 0;
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

            connectionQueue = new ConcurrentQueue<HubConnection>(connections);
            var tasks = new List<Task>();

            var startTimeOffsetGenerator = new RandomGenerator(new LocalFileSaver());

            for (var i = 0; i < _tk.JobConfig.ConcurrentConnections; i++)
            {
                // if (connectionQueue.Count > 0)
                // {
                // Util.Log($"beg: {beg++}");
                if (connectionQueue.TryDequeue(out var connection))
                {
                    await Task.Delay(startTimeOffsetGenerator.Delay(TimeSpan.FromMilliseconds(100)));
                    tasks.Add(StartConnect(connection));
                }
                // }
            }

            // while (connectionQueue.Count > 0)
            // {
            //     await Task.WhenAny(tasks);
            //     Util.Log("deque");
            //     Util.Log($"beg: {beg++} left: {connectionQueue.Count}");
            //     if (connectionQueue.TryDequeue(out var connection)) tasks.Add(StartConnect(connection));
            // }

            // var tasks = new List<Task>();
            // for (var i = 0; i < connections.Count; i++)
            // {
            //     await StartConnect(connections[i]);
            // }

            await Task.WhenAll(tasks);

            swConn.Stop();
            Util.Log($"connection time: {swConn.Elapsed.TotalSeconds} s");

            _tk.State = Stat.Types.State.HubconnConnected;
        }

        private async Task StartConnect(HubConnection connection)
        {
            if (connection == null) return;
            try
            {
                // if (Environment.GetEnvironmentVariable("connect") == "true") File.AppendAllText("connect.txt", $"{Util.Timestamp()} (\n");
                Interlocked.Increment(ref cnt);
                await connection.StartAsync();
                Interlocked.Decrement(ref cnt);
                // if (Environment.GetEnvironmentVariable("connect") == "true") File.AppendAllText("connect.txt", $"{Util.Timestamp()} )\n");

                _tk.Counters.IncreaseConnectionSuccess();
                if (connectionQueue.TryDequeue(out var newConnection))
                {
                    // Util.Log($"beg: {beg++}");
                    await StartConnect(newConnection);
                }
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