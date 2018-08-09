using System;
using System.Collections.Concurrent;
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
    class BaseSendMsgOp : BaseOp
    {
        private IStartTimeOffsetGenerator StartTimeOffsetGenerator;
        private List<int> _sentMessages;
        private WorkerToolkit _tk;
        private List<bool> _brokenConnectionInds;
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
            await StartSendMsg();

            // if (!debug) await Task.Delay(30 * 1000);

            // save counters
            // SaveCounters();

            _tk.State = Stat.Types.State.SendComplete;
            Util.Log($"Sending Complete");
        }

        private void Setup()
        {
            StartTimeOffsetGenerator = new RandomGenerator(new LocalFileSaver());

            _sentMessages = Enumerable.Repeat(0, _tk.JobConfig.Connections).ToList();
            _brokenConnectionInds = Enumerable.Repeat(false, _tk.JobConfig.Connections).ToList();
            Util.Log($"_brokenConnectionInds count: {_brokenConnectionInds.Count}");
            SetCallbacks();

            // _tk.Counters.ResetCounters(withConnection: false);
            if (!_tk.Init) _tk.Counters.ResetCounters(withConnection: false);
            _tk.Init = true;
        }

        private void SetCallbacks()
        {
            Util.Log($"scenario: {_tk.BenchmarkCellConfig.Scenario}");
            for (int i = _tk.ConnectionRange.Begin; i < _tk.ConnectionRange.End; i++)
            {
                var ind = i;

                if (!_tk.Init)
                    _tk.Connections[i - _tk.ConnectionRange.Begin].On(_tk.BenchmarkCellConfig.Scenario,
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

        private async Task StartSendMsg()
        {
            var messageBlob = new byte[_tk.BenchmarkCellConfig.MessageSize];
            Random rnd = new Random();
            rnd.NextBytes(messageBlob);

            var beg = _tk.ConnectionRange.Begin;
            var end = _tk.ConnectionRange.End;

            var sendCnt = 0;
            for (var i = _tk.ConnectionRange.Begin; i < _tk.ConnectionRange.End; i++)
            {
                var cfg = _tk.ConnectionConfigList.Configs[i];
                if (cfg.SendFlag) sendCnt++;
            }
            if (_tk.Connections.Count == 0 || sendCnt == 0)
            {
                Util.Log($"nothing to do, wait scenario finish");
                await Task.Delay(TimeSpan.FromSeconds(_tk.JobConfig.Duration + 5));
            }
            else
            {
                var tasks = new List<Task>();

                for (var i = _tk.ConnectionRange.Begin; i < _tk.ConnectionRange.End; i++)
                {
                    var cfg = _tk.ConnectionConfigList.Configs[i];
                    if (cfg.SendFlag) tasks.Add(StartSendingMessageAsync(_tk.Connections[i - _tk.ConnectionRange.Begin], i - _tk.ConnectionRange.Begin, messageBlob));
                }

                await Task.WhenAll(tasks);
            }
        }

        private async Task StartSendingMessageAsync(HubConnection connection, int ind, byte[] messageBlob)
        {
            var messageSize = (ulong) messageBlob.Length;
            await Task.Delay(StartTimeOffsetGenerator.Delay(TimeSpan.FromSeconds(_tk.JobConfig.Interval)));
            using(var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_tk.JobConfig.Duration)))
            {
                while (!cts.IsCancellationRequested)
                {
                    if (!_brokenConnectionInds[ind])
                    {
                        var id = $"{Util.GuidEncoder.Encode(Guid.NewGuid())}";
                        if (_tk.BenchmarkCellConfig.Scenario.Contains("sendToClient"))
                            id = _tk.BenchmarkCellConfig.TargetConnectionIds[ind + _tk.ConnectionRange.Begin];

                        Task.Run(async() =>
                        {
                            try
                            {
                                var time = $"{Util.Timestamp()}";
                                await connection.SendAsync(_tk.BenchmarkCellConfig.Scenario, id, time, messageBlob);
                                _tk.Counters.IncreaseSentMessageSize(messageSize);
                                _sentMessages[ind]++;
                                _tk.Counters.IncreseSentMsg();
                            }
                            catch (Exception ex)
                            {
                                Util.Log($"exception in sending message of {ind}th connection: {ex}");
                                _tk.Counters.IncreseNotSentFromClientMsg();
                                _brokenConnectionInds[ind] = true;
                            }
                        });

                    }
                    else
                    {
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
    }
}