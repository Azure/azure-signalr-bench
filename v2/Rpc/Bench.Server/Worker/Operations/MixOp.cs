using Bench.Common;
using Bench.RpcSlave.Worker.Savers;
using Bench.RpcSlave.Worker.StartTimeOffsetGenerator;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bench.RpcSlave.Worker.Operations
{
    class MixOp: BaseOp
    {
        private IStartTimeOffsetGenerator StartTimeOffsetGenerator;
        private List<int> _sentMessagesEcho;
        private List<int> _sentMessagesBroadcast;
        private List<int> _sentMessagesGroup;
        private WorkerToolkit _tk;
        
        public void Do(WorkerToolkit tk)
        {
            var waitTime = 5 * 1000;
            Console.WriteLine($"wait time: {waitTime / 1000}s");
            Task.Delay(waitTime).Wait();

            _tk = tk;
            _tk.State = Stat.Types.State.SendReady;

            // setup
            Util.Log($"mix setup");
            Setup();
            Task.Delay(5000).Wait();

            _tk.State = Stat.Types.State.SendRunning;
            Task.Delay(5000).Wait();

            if (_tk.Connections.Count == 0)
            {
                Util.Log("no connections");
                Task.Delay(TimeSpan.FromSeconds(_tk.JobConfig.Duration + 15)).Wait();
            }
            else
            {
                Util.Log($"join group");
                JoinGroup();
                Task.Delay(5000).Wait();

                StartSendMsg();

                Util.Log($"leave group");
                LeaveGroup();
                Task.Delay(5000).Wait();
            }

            Task.Delay(30 * 1000).Wait();

            // save counters
            SaveCounters();

            _tk.State = Stat.Types.State.SendComplete;
            Util.Log($"Sending Complete");

        }

        private void Setup()
        {
            StartTimeOffsetGenerator = new RandomGenerator(new LocalFileSaver());
            _sentMessagesEcho = new List<int>(_tk.BenchmarkCellConfig.MixEchoConnection);
            _sentMessagesBroadcast = new List<int>(_tk.BenchmarkCellConfig.MixBroadcastConnection);
            _sentMessagesGroup = new List<int>(_tk.BenchmarkCellConfig.MixGroupConnection);

            for (var i = 0; i <_tk.BenchmarkCellConfig.MixEchoConnection; i++)
            {
                _sentMessagesEcho.Add(0);
            }
            for (var i = 0; i < _tk.BenchmarkCellConfig.MixBroadcastConnection; i++)
            {
                _sentMessagesBroadcast.Add(0);
            }
            for (var i = 0; i < _tk.BenchmarkCellConfig.MixGroupConnection; i++)
            {
                _sentMessagesGroup.Add(0);
            }

            SetCallbacks();

            _tk.Counters.ResetCounters(withConnection: false);

        }

        private void JoinGroup()
        {
            var echoConnCnt = _tk.BenchmarkCellConfig.MixEchoConnection;
            var broadcastConnCnt = _tk.BenchmarkCellConfig.MixBroadcastConnection;
            var groupConnCnt = _tk.BenchmarkCellConfig.MixGroupConnection;
            (int beg, int end) = GetRange("mix", echoConnCnt, broadcastConnCnt, groupConnCnt);
            for (int i = beg; i < end; i++)
            {
                Task.Run(() =>
                {
                    try
                    {
                        _tk.Connections[i].SendAsync("JoinGroup", _tk.BenchmarkCellConfig.MixGroupName, "");
                    }
                    catch (Exception ex)
                    {
                        Util.Log($"Join group failed: {ex}");
                        _tk.Counters.IncreaseJoinGroupFail();
                    }
                });
            }
        }

        private void LeaveGroup()
        {
            var echoConnCnt = _tk.BenchmarkCellConfig.MixEchoConnection;
            var broadcastConnCnt = _tk.BenchmarkCellConfig.MixBroadcastConnection;
            var groupConnCnt = _tk.BenchmarkCellConfig.MixGroupConnection;
            (int beg, int end) = GetRange("mix", echoConnCnt, broadcastConnCnt, groupConnCnt);
            for (int i = beg; i < end; i++)
            {
                Task.Run(() =>
                {
                    try
                    {
                        _tk.Connections[i].SendAsync("LeaveGroup", _tk.BenchmarkCellConfig.MixGroupName, "");
                    }
                    catch (Exception ex)
                    {
                        Util.Log($"Leave group failed: {ex}");
                        _tk.Counters.IncreaseLeaveGroupFail();
                    }
                });
            }
        }


        private void SetCallbacks()
        {
            for (int i = 0; i < _tk.Connections.Count; i++)
            {
                var callbackName = "";
                var echoConnCnt = _tk.BenchmarkCellConfig.MixEchoConnection;
                var broadcastConnCnt = _tk.BenchmarkCellConfig.MixBroadcastConnection;
                var groupConnCnt = _tk.BenchmarkCellConfig.MixGroupConnection;

                if (IsInRangeOf("echo", i, echoConnCnt, broadcastConnCnt, groupConnCnt)) callbackName = "echo";
                if (IsInRangeOf("broadcast", i, echoConnCnt, broadcastConnCnt, groupConnCnt)) callbackName = "broadcast";
                if (IsInRangeOf("group", i, echoConnCnt, broadcastConnCnt, groupConnCnt)) callbackName = "SendGroup";

                _tk.Connections[i].On(callbackName, (int count, string time) =>
                {
                    var receiveTimestamp = Util.Timestamp();
                    var sendTimestamp = Convert.ToInt64(time);

                    _tk.Counters.CountLatency(sendTimestamp, receiveTimestamp);
                    _tk.Counters.SetServerCounter(count);
                });
            }
        }

        private void StartSendMsg()
        {
            if (_tk.Connections.Count == 0)
            {
                Task.Delay(TimeSpan.FromSeconds(_tk.JobConfig.Duration + 5)).Wait();
            }
            else
            {
                var tasks = new List<Task>(_tk.Connections.Count);
                for (var i = 0; i < _tk.Connections.Count; i++)
                {
                    tasks.Add(StartSendingMessageAsync(_tk.Connections[i], i));
                }

                Task.WhenAll(tasks).Wait();
            }
        }

        private async Task StartSendingMessageAsync(HubConnection connection, int i)
        {
            await Task.Delay(StartTimeOffsetGenerator.Delay(TimeSpan.FromSeconds(_tk.JobConfig.Interval)));

            var echoConnCnt = _tk.BenchmarkCellConfig.MixEchoConnection;
            var broadcastConnCnt = _tk.BenchmarkCellConfig.MixBroadcastConnection;
            var groupConnCnt = _tk.BenchmarkCellConfig.MixGroupConnection;
            var name = "";
            if (IsInRangeOf("echo", i, echoConnCnt, broadcastConnCnt, groupConnCnt)) name = "echo";
            if (IsInRangeOf("broadcast", i, echoConnCnt, broadcastConnCnt, groupConnCnt)) name = "broadcast";
            if (IsInRangeOf("group", i, echoConnCnt, broadcastConnCnt, groupConnCnt)) name = "sendGroup";
            

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_tk.JobConfig.Duration)))
            {
                while (!cts.IsCancellationRequested)
                {
                    try
                    {
                        await connection.SendAsync(name, _tk.BenchmarkCellConfig.MixGroupName, $"{Util.Timestamp()}");
                        if (IsInRangeOf("echo", i, echoConnCnt, broadcastConnCnt, groupConnCnt))
                        {
                            (var beg, var end) = GetRange("echo", echoConnCnt, broadcastConnCnt, groupConnCnt);
                            var ind  = i - beg;
                            _sentMessagesEcho[ind]++;
                        }
                        if (IsInRangeOf("broadcast", i, echoConnCnt, broadcastConnCnt, groupConnCnt))
                        {
                            (var beg, var end) = GetRange("broadcast", echoConnCnt, broadcastConnCnt, groupConnCnt);
                            var ind  = i - beg;
                            _sentMessagesBroadcast[ind]++;
                        }
                        if (IsInRangeOf("group", i, echoConnCnt, broadcastConnCnt, groupConnCnt))
                        {
                            (var beg, var end) = GetRange("group", echoConnCnt, broadcastConnCnt, groupConnCnt);
                            var ind  = i - beg;
                            _sentMessagesGroup[ind]++;
                        }
                        _tk.Counters.IncreseSentMsg();

                    }
                    catch (Exception ex)
                    {
                        Util.Log($"send msg fails: {name}, exception: {ex}");
                        _tk.Counters.IncreseNotSentFromClientMsg();
                    }
                    await Task.Delay(TimeSpan.FromSeconds(_tk.JobConfig.Interval));
                }
            }
        }

        private void SaveCounters()
        {
            _tk.Counters.SaveCounters();
        }

        private (int, int) GetRange(string name, int echoConnCnt, int broadcastConnCnt, int groupConnCnt)
        {
            switch (name)
            {
                case "echo":
                    return (0, echoConnCnt);
                case "broadcast":
                    return (echoConnCnt, echoConnCnt + broadcastConnCnt);
                case "group":
                    return (echoConnCnt + broadcastConnCnt, echoConnCnt + broadcastConnCnt + groupConnCnt);
                default:
                    return (-1,-1);
            }
        }

        private bool IsInRangeOf(string name, int ind, int echoConnCnt, int broadcastConnCnt, int groupConnCnt)
        {
            (int beg, int end) = GetRange(name, echoConnCnt, broadcastConnCnt, groupConnCnt);
            if (ind >= beg && ind < end)
            {
                return true;
            }
            return false;
        }
    }
}
