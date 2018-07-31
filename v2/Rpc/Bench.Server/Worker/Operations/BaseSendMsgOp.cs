using System;
using System.Collections.Generic;
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
        public void Do(WorkerToolkit tk)
        {
            var waitTime = 5 * 1000;
            Console.WriteLine($"wait time: {waitTime / 1000}s");
            Task.Delay(waitTime).Wait();

            _tk = tk;
            _tk.State = Stat.Types.State.SendReady;

            // setup
            Setup();
            Task.Delay(5000).Wait();

            _tk.State = Stat.Types.State.SendRunning;
            Task.Delay(5000).Wait();

            // send message
            StartSendMsg();

            Task.Delay(30 * 1000).Wait();

            // save counters
            SaveCounters();

            _tk.State = Stat.Types.State.SendComplete;
            Util.Log($"Sending Complete");
        }

        private void Setup()
        {
            StartTimeOffsetGenerator = new RandomGenerator(new LocalFileSaver());

            _sentMessages = new List<int>(_tk.JobConfig.Connections);
            for (int i = 0; i < _tk.JobConfig.Connections; i++)
            {
                _sentMessages.Add(0);
            }

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
                            _tk.Counters.CountLatency(sendTimestamp, receiveTimestamp);
                            _tk.Counters.SetServerCounter(count);
                        });

            }
        }

        private void StartSendMsg()
        {
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
                var tasks = new List<Task>();
                for (var i = _tk.ConnectionRange.Begin; i < _tk.ConnectionRange.End; i++)
                {
                    var cfg = _tk.ConnectionConfigList.Configs[i];
                    if (cfg.SendFlag) tasks.Add(StartSendingMessageAsync(_tk.Connections[i - _tk.ConnectionRange.Begin], i - _tk.ConnectionRange.Begin));
                }

                Task.WhenAll(tasks).Wait();
            }
        }

        private async Task StartSendingMessageAsync(HubConnection connection, int ind)
        {
            await Task.Delay(StartTimeOffsetGenerator.Delay(TimeSpan.FromSeconds(_tk.JobConfig.Interval)));
            using(var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_tk.JobConfig.Duration)))
            {
                while (!cts.IsCancellationRequested)
                {
                    try
                    {
                        if (_tk.BenchmarkCellConfig.Scenario.Contains("sendToClient"))
                        {
                            await connection.SendAsync(_tk.BenchmarkCellConfig.Scenario, _tk.BenchmarkCellConfig.TargetConnectionIds[ind + _tk.ConnectionRange.Begin], $"{Util.Timestamp()}", messageBlob);
                        }
                        else
                        {
                            await connection.SendAsync(_tk.BenchmarkCellConfig.Scenario, $"{Util.GuidEncoder.Encode(Guid.NewGuid())}", $"{Util.Timestamp()}", messageBlob);
                        }
                        _sentMessages[ind]++;
                        _tk.Counters.IncreseSentMsg();

                    }
                    catch
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