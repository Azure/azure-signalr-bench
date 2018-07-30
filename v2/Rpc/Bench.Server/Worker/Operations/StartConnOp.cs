using Bench.Common;
using Bench.Common.Config;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Bench.RpcSlave.Worker.Operations
{
    class StartConnOp : BaseOp, IOperation
    {
        private WorkerToolkit _tk;
        public void Do(WorkerToolkit tk)
        {
            _tk = tk;
            Start(tk.Connections);
        }

        private void Start(List<HubConnection> connections)
        {
            Util.Log($"start connections");
            _tk.State = Stat.Types.State.HubconnConnecting;


            var swConn = new Stopwatch();
            swConn.Start();
            //int concurrency = 50;
            //var tasks = new List<Task>(connections.Count);
            //var i = 0;
            //foreach (var conn in connections)
            //{
            //    tasks.Add(Task.Run(() =>
            //    {
            //        try
            //        {
            //            conn.StartAsync().Wait();
            //        }
            //        catch (Exception ex)
            //        {
            //            Util.Log($"start connection exception: {ex}");
            //            _tk.Counters.IncreaseConnectionError();
            //        }
            //    }));


            //    if (i > 0 && i % concurrency == 0)
            //    {
            //        Task.Delay(TimeSpan.FromSeconds(1)).Wait();
            //    }
            //}
            //Task.WhenAll(tasks).Wait();
            Util.Log($"concurrent conn: {_tk.JobConfig.ConcurrentConnections}");
            var left = connections.Count;
            var nextBatch = _tk.JobConfig.ConcurrentConnections; // bug: concurrent could be 0 -> dead loop
            if (nextBatch <= left)
            {
                var tasks = new List<Task>(connections.Count);
                var i = 0;
                do
                {
                    for (var j = 0; j < nextBatch; j++)
                    {
                        var index = i + j;
                        tasks.Add(Task.Run(() =>
                        {
                            try
                            {
                                connections[index].StartAsync().Wait();
                            }
                            catch (Exception ex)
                            {
                                Util.Log($"start connection exception: {ex}");
                                _tk.Counters.IncreaseConnectionError();
                            }
                        }));
                    }

                    Task.Delay(TimeSpan.FromSeconds(1)).Wait();
                    i += nextBatch;
                    left = left - nextBatch;
                    if (left < nextBatch)
                    {
                        nextBatch = left;
                    }
                } while (left > 0);
                Task.WhenAll(tasks).Wait();
            }

            _tk.Counters.UpdateConnectionSuccess(_tk.Connections.Count);
            swConn.Stop();
            Util.Log($"connection time: {swConn.Elapsed.TotalSeconds}");

            _tk.State = Stat.Types.State.HubconnConnected;
        }
    }
}
