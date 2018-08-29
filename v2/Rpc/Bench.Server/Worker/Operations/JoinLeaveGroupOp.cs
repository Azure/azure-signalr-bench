using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bench.Common;
using Bench.RpcSlave.Worker.Counters;
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

            var beg = _tk.ConnectionRange.Begin;
            var end = _tk.ConnectionRange.End;
            await JoinLeaveGroup(_tk.BenchmarkCellConfig.Step, _tk.Connections, _tk.BenchmarkCellConfig.GroupNameList.ToList().GetRange(beg, end - beg), _tk.Counters);

            _tk.State = Stat.Types.State.SendComplete;
            Util.Log($"Sending Complete");

        }

        protected void Setup()
        {

            if (!_tk.Init.ContainsKey(_tk.BenchmarkCellConfig.Step))
            {
                SetCallbacks(_tk.BenchmarkCellConfig.Step, _tk.Connections, _tk.Counters);
                _tk.Init[_tk.BenchmarkCellConfig.Step] = true;
            }

        }

        public static async Task JoinLeaveGroup(string mode, List<HubConnection> connections, List<string> groupNameMatrix, Counter counter)
        {
            var tasks = new List<Task>();
            for (var i = 0; i < connections.Count; i++)
            {
                var ind = i;
                tasks.Add(
                    Task.Run(async() =>
                    {
                        var groupNameList = groupNameMatrix[ind].Split(";");
                        for (var j = 0; j < groupNameList.Length; j++)
                        {
                            try
                            {
                                await connections[ind].SendAsync(mode, groupNameList[j], "perf");
                            }
                            catch (Exception ex)
                            {
                                Util.Log($"{mode} failed: {ex}");
                                if (mode.ToLower().Contains("join"))
                                    counter.IncreaseJoinGroupFail();
                                else
                                    counter.IncreaseLeaveGroupFail();
                            }
                        }
                    })
                );
            }
            await Task.WhenAll(tasks);
        }

        public static void SetCallbacks(string mode, List<HubConnection> connections, Counter counter)
        {
            Util.Log($"SetCallbacks step: {mode}");
            for (int i = 0; i < connections.Count; i++)
            {
                var ind = i;
                var callbackName = mode.First().ToString().ToUpper() + mode.Substring(1);
                connections[i].On(callbackName,
                    (string thisId, string message) =>
                    {
                        if (mode.ToLower().Contains("join"))
                        {
                            counter.IncreaseJoinGroupSuccess();
                        }
                        else
                        {
                            counter.IncreaseLeaveGroupSuccess();
                        }
                    });

            }
        }

    }
}