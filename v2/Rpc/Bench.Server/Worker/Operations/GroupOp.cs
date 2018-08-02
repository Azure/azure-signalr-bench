using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bench.Common;
using Bench.RpcSlave.Worker.Savers;
using Bench.RpcSlave.Worker.StartTimeOffsetGenerator;
using Microsoft.AspNetCore.SignalR.Client;

namespace Bench.RpcSlave.Worker.Operations
{
    class GroupOp : BaseOp
    {
        private IStartTimeOffsetGenerator StartTimeOffsetGenerator;
        private List<int> _sentMessagesGroup;
        private WorkerToolkit _tk;
        private List<bool> _brokenConnectionInds;

        public void Do(WorkerToolkit tk)
        {
            var debug = Environment.GetEnvironmentVariable("debug") == "debug" ? true : false;

            var waitTime = 5 * 1000;
            if (!debug) Console.WriteLine($"wait time: {waitTime / 1000}s");
            if (!debug) Task.Delay(waitTime).Wait();

            _tk = tk;
            _tk.State = Stat.Types.State.SendReady;

            // setup
            Util.Log($"group setup");
            Setup();
            if (!debug) Task.Delay(5000).Wait();

            _tk.State = Stat.Types.State.SendRunning;
            if (!debug) Task.Delay(5000).Wait();

            var sendCnt = 0;
            for (var i = _tk.ConnectionRange.Begin; i < _tk.ConnectionRange.End; i++)
            {
                var cfg = _tk.ConnectionConfigList.Configs[i];
                if (cfg.SendFlag) sendCnt++;
            }

            if (_tk.Connections.Count == 0 || sendCnt == 0)
            {
                Task.Delay(TimeSpan.FromSeconds(_tk.JobConfig.Duration + 5)).Wait();
            }
            else
            {
                Util.Log($"join group");
                JoinGroup();
                if (!debug) Task.Delay(5000).Wait();

                StartSendMsg();

                Util.Log($"leave group");
                LeaveGroup();
                if (!debug) Task.Delay(5000).Wait();
            }

            if (!debug) Task.Delay(30 * 1000).Wait();

            // save counters
            SaveCounters();

            _tk.State = Stat.Types.State.SendComplete;
            Util.Log($"Sending Complete");

        }

        private void Setup()
        {
            StartTimeOffsetGenerator = new RandomGenerator(new LocalFileSaver());
            _brokenConnectionInds = Enumerable.Repeat(false, _tk.JobConfig.Connections).ToList();
            _sentMessagesGroup = Enumerable.Repeat(0, _tk.JobConfig.Connections).ToList();

            SetCallbacks();

            _tk.Counters.ResetCounters(withConnection: false);

        }

        private void JoinGroup()
        {
            var tasks = new List<Task>();
            for (var i = _tk.ConnectionRange.Begin; i < _tk.ConnectionRange.End; i++)
            {
                var ind = i;
                tasks.Add(
                    Task.Run(() =>
                    {
                        var groupNameList = _tk.BenchmarkCellConfig.GroupNameList[ind].Split(";");
                        for (var j = 0; j < groupNameList.Length; j++)
                        {
                            try
                            {
                                _tk.Connections[ind - _tk.ConnectionRange.Begin].SendAsync("JoinGroup", groupNameList[j], "").Wait(); // todo: await to catch exception
                            }
                            catch (Exception ex)
                            {
                                Util.Log($"Join group failed: {ex}");
                                _tk.Counters.IncreaseJoinGroupFail();
                            }
                        }
                    })
                );
            }
            Task.WhenAll(tasks).Wait();
        }

        private void LeaveGroup()
        {
            var tasks = new List<Task>();
            for (int i = _tk.ConnectionRange.Begin; i < _tk.ConnectionRange.End; i++)
            {
                var ind = i;
                tasks.Add(Task.Run(() =>
                {
                    var groupNameList = _tk.BenchmarkCellConfig.GroupNameList[ind].Split(";");
                    for (var j = 0; j < groupNameList.Length; j++)
                    {
                        try
                        {
                            _tk.Connections[ind - _tk.ConnectionRange.Begin].SendAsync("LeaveGroup", groupNameList[j], ""); // todo: await to catch exception
                        }
                        catch (Exception ex)
                        {
                            Util.Log($"Leave group failed: {ex}");
                            _tk.Counters.IncreaseLeaveGroupFail();
                        }
                    }
                }));
            }
            Task.WhenAll(tasks).Wait();

        }

        private void SetCallbacks()
        {
            for (int i = 0; i < _tk.Connections.Count; i++)
            {
                var callbackName = "SendGroup";

                _tk.Connections[i].On(callbackName, (int count, string time) =>
                {
                    var receiveTimestamp = Util.Timestamp();
                    var sendTimestamp = Convert.ToInt64(time);

                    _tk.Counters.CountLatency(sendTimestamp, receiveTimestamp);
                    _tk.Counters.SetServerCounter((ulong) count);
                });

                _tk.Connections[i].On("JoinGroup", (string connectionId, string message) => { });

                _tk.Connections[i].On("LeaveGroup", (string connectionId, string message) => { });
            }
        }

        private void StartSendMsg()
        {

            var tasks = new List<Task>(_tk.Connections.Count);
            for (var i = _tk.ConnectionRange.Begin; i < _tk.ConnectionRange.End; i++)
            {
                var cfg = _tk.ConnectionConfigList.Configs[i];
                if (cfg.SendFlag) tasks.Add(StartSendingMessageAsync(_tk.Connections[i - _tk.ConnectionRange.Begin], i));
            }

            Task.WhenAll(tasks).Wait();
        }

        private async Task StartSendingMessageAsync(HubConnection connection, int i)
        {
            await Task.Delay(StartTimeOffsetGenerator.Delay(TimeSpan.FromSeconds(_tk.JobConfig.Interval)));

            var name = "sendGroup";

            using(var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_tk.JobConfig.Duration)))
            {
                while (!cts.IsCancellationRequested)
                {

                    var groupNameList = _tk.BenchmarkCellConfig.GroupNameList[i].Split(";");
                    for (var j = 0; j < groupNameList.Length; j++)
                    {
                        if (!_brokenConnectionInds[i - _tk.ConnectionRange.Begin])
                        {
                            try
                            {
                                var groupName = groupNameList[j];
                                await connection.SendAsync(name, groupName, $"{Util.Timestamp()}");
                                _sentMessagesGroup[i - _tk.ConnectionRange.Begin]++;
                                _tk.Counters.IncreseSentMsg();
                            }
                            catch (Exception ex)
                            {
                                Util.Log($"send msg fails: {name}, exception: {ex}");
                                _tk.Counters.IncreseNotSentFromClientMsg();
                                _brokenConnectionInds[i - _tk.ConnectionRange.Begin] = true;
                            }
                        }
                        else
                        {
                            _tk.Counters.IncreseNotSentFromClientMsg();
                        }
                    }

                    await Task.Delay(TimeSpan.FromSeconds(_tk.JobConfig.Interval));
                }
            }
        }

        private void SaveCounters()
        {
            _tk.Counters.SaveCounters();
        }
    }
}