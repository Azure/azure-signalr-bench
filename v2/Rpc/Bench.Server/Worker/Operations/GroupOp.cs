using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public async Task Do(WorkerToolkit tk)
        {
            throw new NotImplementedException();
            
            var debug = Environment.GetEnvironmentVariable("debug") == "debug" ? true : false;

            var waitTime = 5 * 1000;
            if (!debug) Console.WriteLine($"wait time: {waitTime / 1000}s");
            if (!debug) await Task.Delay(waitTime);

            _tk = tk;
            _tk.State = Stat.Types.State.SendReady;

            // setup
            Util.Log($"group setup");
            Setup();
            if (!debug) await Task.Delay(5000);

            _tk.State = Stat.Types.State.SendRunning;
            if (!debug) await Task.Delay(5000);

            var sendCnt = 0;
            for (var i = _tk.ConnectionRange.Begin; i < _tk.ConnectionRange.End; i++)
            {
                var cfg = _tk.ConnectionConfigList.Configs[i];
                if (cfg.SendFlag) sendCnt++;
            }

            if (_tk.Connections.Count == 0 || sendCnt == 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(_tk.JobConfig.Duration + 5));
            }
            else
            {
                Util.Log($"join group");
                await JoinGroup();
                if (!debug) await Task.Delay(5000);

                await StartSendMsg();

                Util.Log($"leave group");
                await LeaveGroup();
                if (!debug) await Task.Delay(5000);
            }

            if (!debug) await Task.Delay(30 * 1000);

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

        private async Task JoinGroup()
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
                                await _tk.Connections[ind - _tk.ConnectionRange.Begin].SendAsync("JoinGroup", groupNameList[j], "");
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
            await Task.WhenAll(tasks);
            sw.Stop();
            Util.Log($"join group time : {sw.Elapsed.TotalMilliseconds}");
        }

        private async Task LeaveGroup()
        {
            var tasks = new List<Task>();
            for (int i = _tk.ConnectionRange.Begin; i < _tk.ConnectionRange.End; i++)
            {
                var ind = i;
                tasks.Add(Task.Run(async() =>
                {
                    var groupNameList = _tk.BenchmarkCellConfig.GroupNameList[ind].Split(";");
                    for (var j = 0; j < groupNameList.Length; j++)
                    {
                        try
                        {
                            await _tk.Connections[ind - _tk.ConnectionRange.Begin].SendAsync("LeaveGroup", groupNameList[j], "");
                        }
                        catch (Exception ex)
                        {
                            Util.Log($"Leave group failed: {ex}");
                            _tk.Counters.IncreaseLeaveGroupFail();
                        }
                    }
                }));
            }
            await Task.WhenAll(tasks);

        }

        private void SetCallbacks()
        {
            for (int i = 0; i < _tk.Connections.Count; i++)
            {
                var callbackName = "SendGroup";

                _tk.Connections[i].On(callbackName, (int count, string time, byte[] messageBlob) =>
                {
                    // Util.Log($"group receive...");
                    var receiveTimestamp = Util.Timestamp();
                    var sendTimestamp = Convert.ToInt64(time);
                    var receiveSize = messageBlob.Length * sizeof(byte);

                    _tk.Counters.CountLatency(sendTimestamp, receiveTimestamp);
                    _tk.Counters.SetServerCounter((ulong) count);
                    _tk.Counters.IncreaseReceivedMessageSize((ulong) receiveSize);
                });

                _tk.Connections[i].On("JoinGroup", (string connectionId, string message) => { });

                _tk.Connections[i].On("LeaveGroup", (string connectionId, string message) => { });
            }
        }

        private async Task StartSendMsg()
        {

            var messageBlob = new byte[_tk.BenchmarkCellConfig.MessageSize];
            Random rnd = new Random();
            rnd.NextBytes(messageBlob);

            var tasks = new List<Task>(_tk.Connections.Count);
            for (var i = _tk.ConnectionRange.Begin; i < _tk.ConnectionRange.End; i++)
            {
                var cfg = _tk.ConnectionConfigList.Configs[i];
                if (cfg.SendFlag) tasks.Add(StartSendingMessageAsync(_tk.Connections[i - _tk.ConnectionRange.Begin], i, messageBlob));
            }

            await Task.WhenAll(tasks);
        }

        private async Task StartSendingMessageAsync(HubConnection connection, int i, byte[] messageBlob)
        {
            var messageSize = (ulong) messageBlob.Length;

            await Task.Delay(StartTimeOffsetGenerator.Delay(TimeSpan.FromSeconds(_tk.JobConfig.Interval)));

            var name = "sendGroup";

            using(var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_tk.JobConfig.Duration)))
            {
                while (!cts.IsCancellationRequested)
                {

                    var groupNameList = _tk.BenchmarkCellConfig.GroupNameList[i].Split(";");
                    for (var j = 0; j < groupNameList.Length; j++)
                    {
                        var jInd = j;
                        if (!_brokenConnectionInds[i - _tk.ConnectionRange.Begin])
                        {
                            Task.Run(async() =>
                            {
                                try
                                {

                                    // Util.Log($"senddddd");
                                    var groupName = groupNameList[jInd];
                                    await connection.SendAsync(name, groupName, $"{Util.Timestamp()}", messageBlob);
                                    _tk.Counters.IncreaseSentMessageSize(messageSize);
                                    _sentMessagesGroup[i - _tk.ConnectionRange.Begin]++;
                                    _tk.Counters.IncreseSentMsg();
                                }
                                catch (Exception ex)
                                {
                                    Util.Log($"send msg fails: {name}, exception: {ex}");
                                    _tk.Counters.IncreseNotSentFromClientMsg();
                                    _brokenConnectionInds[i - _tk.ConnectionRange.Begin] = true;
                                }
                            });

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