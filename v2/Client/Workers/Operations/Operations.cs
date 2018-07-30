using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Text;
using Client.UtilNs;
using Client.Statistics;
using Client.Tools;
using System.Threading.Tasks;
using Client.ClientJobNs;
using Client.StartTimeOffsetGenerator;
using Client.Statistics.Savers;
using Interlocked = System.Threading.Interlocked;
using System.Threading;
using System.Diagnostics;
using System.Linq;

namespace Client.Workers.OperationsNs
{
    class BaseOp
    {
        
    }

    class EchoOp: BaseOp, IOperation
    {
        public ICounters Counters { get; set; } = new Counters(new LocalFileSaver());
        public List<System.Timers.Timer> TimerPerConnection;
        public List<TimeSpan> DelayPerConnection;
        private BaseTool _pkg;
        public IStartTimeOffsetGenerator StartTimeOffsetGenerator;
        public int totalSentMsg = 0;
        public int totalErrMsg = 0;
        public int totalReceivedMsg = 0;

        public EchoOp(BaseTool pkg)
        {
            _pkg = pkg;
            StartTimeOffsetGenerator = new RandomGenerator(new LocalFileSaver());
        }

        public void Setup()
        {
            SetCallbacks();
            //SetTimers();

            for(int i = 0; i < _pkg.SentMassage.Count; i++)
            {
                _pkg.SentMassage[i] = 0;
            }
        }


        public void Process()
        {
            StartSendMsg();
        }

        private void SetCallbacks()
        {
            for (int i = 0; i < _pkg.Connections.Count; i++)
            {
                int ind = i;
                _pkg.Connections[i].On(_pkg.Job.CallbackName, (string uid, string time) =>
                {
                    var receiveTimestamp = Util.Timestamp();
                    var sendTimestamp = Convert.ToInt64(time);
                    //Util.Log($"diff time: {receiveTimestamp - sendTimestamp}");
                    Counters.CountLatency(sendTimestamp, receiveTimestamp);
                    Interlocked.Increment(ref totalReceivedMsg);
                    if (ind == 0) Util.Log($"#### echocallback");
                });
            }
        }

        public void StartSendMsg()
        {
            //for (int i = 0; i < _pkg.Connections.Count; i++)
            //{
            //    int ind = i;
            //    _ = Task.Delay(DelayPerConnection[i]).ContinueWith(_ =>
            //    {
            //        TimerPerConnection[ind].Start();
            //    });
            //}

            var tasks = _pkg.Connections.Select(StartSendingMessageAsync).ToList();
            Task.WhenAll(tasks).Wait();
            Util.Log($"msg send: {totalSentMsg}, receive: {totalReceivedMsg}, not sent: {totalErrMsg}");
        }

        //protected void SetTimers()
        //{
        //    TimerPerConnection = new List<System.Timers.Timer>(_pkg.Job.Connections);
        //    DelayPerConnection = new List<TimeSpan>(_pkg.Job.Connections);

        //    for (int i = 0; i < _pkg.Connections.Count; i++)
        //    {
        //        var delay = StartTimeOffsetGenerator.Delay(TimeSpan.FromSeconds(_pkg.Job.Interval));
        //        DelayPerConnection.Add(delay);

        //        TimerPerConnection.Add(new System.Timers.Timer());

        //        var ind = i;
        //        var startTime = Util.Timestamp();
        //        TimerPerConnection[i].AutoReset = true;
        //        TimerPerConnection[i].Elapsed += (sender, e) =>
        //        {
        //            // set new interval
        //            TimerPerConnection[ind].Stop();
        //            TimerPerConnection[ind].Interval = _pkg.Job.Interval * 1000;
        //            TimerPerConnection[ind].Start();

        //            if (_pkg.SentMassage[ind] >= _pkg.Job.Duration * _pkg.Job.Interval)
        //            {
        //                TimerPerConnection[ind].Stop();
        //                return;
        //            }

        //            if (ind == 0)
        //            {
        //                Util.Log($"Sending Message");
        //            }

        //            try
        //            {
        //                _pkg.Connections[ind].SendAsync("echo", $"{GuidEncoder.Encode(Guid.NewGuid())}", $"{Util.Timestamp()}").Wait();
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine($"Failed to send massage: {ex} \n");
        //            }
        //            Interlocked.Increment(ref totalSentMsg);
        //            _pkg.SentMassage[ind]++;
        //            Counters.IncreseSentMsg();

        //        };
        //    }


        //}

        private async Task StartSendingMessageAsync(HubConnection connection)
        {
            await Task.Delay(StartTimeOffsetGenerator.Delay(TimeSpan.FromSeconds(_pkg.Job.Interval)));
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_pkg.Job.Duration)))
            {
                while (!cts.IsCancellationRequested)
                {
                    try
                    {
                        await connection.SendAsync("echo", "id", $"{Util.Timestamp()}");
                        Interlocked.Increment(ref totalSentMsg);
                    }
                    catch
                    {
                        Interlocked.Increment(ref totalErrMsg);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(_pkg.Job.Interval));
                }
            }
        }


        public void SaveCounters()
        {
            Counters.SaveCounters();
        }
    }
}
