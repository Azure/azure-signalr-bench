using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bench.Common;
using Bench.RpcSlave.Worker.Savers;
using Bench.RpcSlave.Worker.StartTimeOffsetGenerator;
using Microsoft.AspNetCore.SignalR.Client;

namespace Bench.RpcSlave.Worker.Operations
{
    class JoinLeaveGroupOp : BaseOp
    {
        private WorkerToolkit _tk;

        public JoinLeaveGroupOp()
        {
            var opName = GetType().Name;
            Util.Log(opName.Substring(0, opName.Length - 2) + " Operation Started.");
        }
        public async Task Do(WorkerToolkit tk)
        {
            var debug = Environment.GetEnvironmentVariable("debug") == "debug" ? true : false;

            var waitTime = 5 * 1000;
            if (!debug) Console.WriteLine($"wait time: {waitTime / 1000}s");
            if (!debug) await Task.Delay(waitTime);

            _tk = tk;
            _tk.State = Stat.Types.State.SendReady;

            // setup
            Setup();
            if (!debug) await Task.Delay(5000);

            _tk.State = Stat.Types.State.SendRunning;
            if (!debug) await Task.Delay(5000);

            // send message
            await JoinLeaveGroup();

            _tk.State = Stat.Types.State.SendComplete;
            Util.Log($"Sending Complete");

        }

        private void Setup()
        {

            SetCallbacks();

        }

        private async Task JoinLeaveGroup()
        {
            var sw = new Stopwatch();
            sw.Start();
            var tasks = new List<Task>();
            for (var i = _tk.ConnectionRange.Begin; i < _tk.ConnectionRange.End; i++)
            {
                var ind = i;
                tasks.Add(
                    Task.Run(async() =>
                    {
                        var groupNameList = _tk.BenchmarkCellConfig.GroupNameList[ind].Split(";");
                        for (var j = 0; j < groupNameList.Length; j++)
                        {
                            try
                            {
                                // Util.Log($"join group {groupNameList[j]}");
                                await _tk.Connections[ind - _tk.ConnectionRange.Begin].SendAsync(_tk.BenchmarkCellConfig.Step, groupNameList[j], "");
                            }
                            catch (Exception ex)
                            {
                                Util.Log($"{_tk.BenchmarkCellConfig.Step} failed: {ex}");
                                _tk.Counters.IncreaseJoinGroupFail();
                            }
                        }
                    })
                );
            }
            await Task.WhenAll(tasks);
            sw.Stop();
            Util.Log($"{_tk.BenchmarkCellConfig.Step} time : {sw.Elapsed.TotalMilliseconds} ms");
        }

        private void SetCallbacks()
        {
            Util.Log($"step: {_tk.BenchmarkCellConfig.Step}");
            for (int i = _tk.ConnectionRange.Begin; i < _tk.ConnectionRange.End; i++)
            {
                var ind = i;

                if (!_tk.Init)
                    _tk.Connections[i - _tk.ConnectionRange.Begin].On(_tk.BenchmarkCellConfig.Step,
                        (int count, string time, string thisId, string targetId, byte[] messageBlob) =>
                        {
                            var receiveTimestamp = Util.Timestamp();
                            var sendTimestamp = Convert.ToInt64(time);
                            var receiveSize = messageBlob != null ? messageBlob.Length * sizeof(byte) : 0;
                            _tk.Counters.CountLatency(sendTimestamp, receiveTimestamp);
                            _tk.Counters.SetServerCounter(((ulong) count));
                            _tk.Counters.IncreaseReceivedMessageSize((ulong) receiveSize);
                        });
            }
        }

    }
}