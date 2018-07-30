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
    class LeaveGroupOp : BaseOp
    {
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

            if (_tk.Connections.Count == 0 || _tk.ConnectionRange.End - _tk.ConnectionRange.Begin == 0 || _tk.BenchmarkCellConfig.Scenario != "group")
            {
                Util.Log ("no connections");
                // Task.Delay (TimeSpan.FromSeconds (_tk.JobConfig.Duration + 15)).Wait ();
            }
            else
            {

                Util.Log ($"leave group");
                LeaveGroup ();
                // Task.Delay (5000).Wait ();
            }

            // Task.Delay (30 * 1000).Wait ();

            // save counters
            // SaveCounters ();

            _tk.State = Stat.Types.State.SendComplete;
            Util.Log ($"Sending Complete");

        }

        private void Setup ()
        {
            SetCallbacks ();

            _tk.Counters.ResetCounters (withConnection: false);

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
                _tk.Connections[i].On ("LeaveGroup", (string connectionId, string message) => { });
            }
        }

        private void SaveCounters ()
        {
            _tk.Counters.SaveCounters ();
        }
    }
}