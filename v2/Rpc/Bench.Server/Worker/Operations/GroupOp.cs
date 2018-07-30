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
    class GroupOp : BaseOp
    {
        private IStartTimeOffsetGenerator StartTimeOffsetGenerator;
        private List<int> _sentMessagesGroup;
        private WorkerToolkit _tk;

        public void Do (WorkerToolkit tk)
        {
            var waitTime = 5 * 1000;
            Console.WriteLine ($"wait time: {waitTime / 1000}s");
            Task.Delay (waitTime).Wait ();

            _tk = tk;
            _tk.State = Stat.Types.State.SendReady;

            // setup
            Util.Log ($"group setup");
            Setup ();
            Task.Delay (5000).Wait ();

            _tk.State = Stat.Types.State.SendRunning;
            Task.Delay (5000).Wait ();

            if (_tk.Connections.Count == 0)
            {
                Util.Log ("no connections");
                Task.Delay (TimeSpan.FromSeconds (_tk.JobConfig.Duration + 15)).Wait ();
            }
            else
            {
                Util.Log ($"join group");
                JoinGroup ();
                Task.Delay (5000).Wait ();

                StartSendMsg ();

                Util.Log ($"leave group");
                LeaveGroup ();
                Task.Delay (5000).Wait ();
            }

            Task.Delay (30 * 1000).Wait ();

            // save counters
            SaveCounters ();

            _tk.State = Stat.Types.State.SendComplete;
            Util.Log ($"Sending Complete");

        }

        private void Setup ()
        {
            StartTimeOffsetGenerator = new RandomGenerator (new LocalFileSaver ());
            _sentMessagesGroup = new List<int> (_tk.Connections.Count);

            for (var i = 0; i < _tk.Connections.Count; i++)
            {
                _sentMessagesGroup.Add (0);
            }

            SetCallbacks ();

            _tk.Counters.ResetCounters (withConnection: false);

        }

        private void JoinGroup ()
        {
            for (var i = _tk.ConnectionRange.Begin; i < _tk.ConnectionRange.End; i++)
            {
                var ind = i;
                Task.Run (() =>
                {
                    try
                    {
                        _tk.Connections[ind - _tk.ConnectionRange.Begin].SendAsync ("JoinGroup", _tk.ConnectionConfigList.Configs[ind].GroupName, "");
                    }
                    catch (Exception ex)
                    {
                        Util.Log ($"Join group failed: {ex}");
                        _tk.Counters.IncreaseJoinGroupFail ();
                    }
                });
            }
        }

        private void LeaveGroup ()
        {
            for (int i = _tk.ConnectionRange.Begin; i < _tk.ConnectionRange.End; i++)
            {
                var ind = i;
                Task.Run (() =>
                {
                    try
                    {
                        _tk.Connections[ind - _tk.ConnectionRange.Begin].SendAsync ("LeaveGroup", _tk.ConnectionConfigList.Configs[ind].GroupName, "");
                    }
                    catch (Exception ex)
                    {
                        Util.Log ($"Leave group failed: {ex}");
                        _tk.Counters.IncreaseLeaveGroupFail ();
                    }
                });
            }
        }

        private void SetCallbacks ()
        {
            for (int i = 0; i < _tk.Connections.Count; i++)
            {
                var callbackName = "SendGroup";

                _tk.Connections[i].On (callbackName, (int count, string time) =>
                {
                    var receiveTimestamp = Util.Timestamp ();
                    var sendTimestamp = Convert.ToInt64 (time);

                    _tk.Counters.CountLatency (sendTimestamp, receiveTimestamp);
                    _tk.Counters.SetServerCounter (count);
                });

                _tk.Connections[i].On ("JoinGroup", (string connectionId, string message) =>
                { });

                _tk.Connections[i].On ("LeaveGroup", (string connectionId, string message) =>
                { });
            }
        }

        private void StartSendMsg ()
        {
            if (_tk.Connections.Count == 0)
            {
                Task.Delay (TimeSpan.FromSeconds (_tk.JobConfig.Duration + 5)).Wait ();
            }
            else
            {
                var tasks = new List<Task> (_tk.Connections.Count);
                for (var i = _tk.ConnectionRange.Begin; i < _tk.ConnectionRange.End; i++)
                {
                    Util.Log ($"tk conn count: {_tk.Connections.Count}, ({_tk.ConnectionRange.Begin}, {_tk.ConnectionRange.End}), i:{i}");
                    tasks.Add (StartSendingMessageAsync (_tk.Connections[i - _tk.ConnectionRange.Begin], i));
                }

                Task.WhenAll (tasks).Wait ();
            }
        }

        private async Task StartSendingMessageAsync (HubConnection connection, int i)
        {
            await Task.Delay (StartTimeOffsetGenerator.Delay (TimeSpan.FromSeconds (_tk.JobConfig.Interval)));

            var name = "sendGroup";

            using (var cts = new CancellationTokenSource (TimeSpan.FromSeconds (_tk.JobConfig.Duration)))
            {
                while (!cts.IsCancellationRequested)
                {
                    try
                    {
                        await connection.SendAsync (name, _tk.ConnectionConfigList.Configs[i - _tk.ConnectionRange.Begin].GroupName, $"{Util.Timestamp()}");
                        _sentMessagesGroup[i - _tk.ConnectionRange.Begin]++;
                        _tk.Counters.IncreseSentMsg ();
                    }
                    catch (Exception ex)
                    {
                        Util.Log ($"send msg fails: {name}, exception: {ex}");
                        _tk.Counters.IncreseNotSentFromClientMsg ();
                    }
                    await Task.Delay (TimeSpan.FromSeconds (_tk.JobConfig.Interval));
                }
            }
        }

        private void SaveCounters ()
        {
            _tk.Counters.SaveCounters ();
        }
    }
}