using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bench.Common;
using Bench.Common.Config;
using Bench.RpcSlave.Worker.Counters;
using Bench.RpcSlave.Worker.Savers;
using Bench.RpcSlave.Worker.StartTimeOffsetGenerator;
using Microsoft.AspNetCore.SignalR.Client;

namespace Bench.RpcSlave.Worker.Operations
{
    class FreqJoinLeaveGroupOp : BaseSendMsgOp, IOperation
    {
        public override async Task StartSendMsg()
        {
            var messageBlob = new byte[_tk.BenchmarkCellConfig.MessageSize];
            Random rnd = new Random();
            rnd.NextBytes(messageBlob);

            var tasks = new List<Task>();

            var beg = _tk.ConnectionRange.Begin;
            var end = _tk.ConnectionRange.End;

            // join group at the begining if the scenario is not frequently join and leave group
            if (_tk.BenchmarkCellConfig.EnableGroupJoinLeave) await JoinLeaveGroupOp.JoinLeaveGroup("JoinGroup", _tk.Connections, _tk.BenchmarkCellConfig.GroupNameList.ToList(), _tk.Counters);

            for (var i = _tk.ConnectionRange.Begin; i < _tk.ConnectionRange.End; i++)
            {
                var cfg = _tk.ConnectionConfigList.Configs[i];
                var ids = GenerateId("SendGroup", i - _tk.ConnectionRange.Begin);
                if (cfg.SendFlag)
                {
                    for (var j = 0; j < ids.Count; j++)
                    {
                        for (var k = 0; k < _tk.BenchmarkCellConfig.MessageCountPerInterval; k++)
                        {
                            tasks.Add(StartSendingMessageAsync("SendGroup", _tk.Connections[i - _tk.ConnectionRange.Begin],
                                i - _tk.ConnectionRange.Begin, messageBlob, ids[j], _tk.Connections.Count,
                                _tk.JobConfig.Duration, _tk.JobConfig.Interval, _tk.Counters, _brokenConnectionInds));
                        }
                    }
                }
                else if (_tk.BenchmarkCellConfig.EnableGroupJoinLeave)
                {
                    tasks.Add(StartJoinLeaveGroupAsync(_tk.Connections.GetRange(i - _tk.ConnectionRange.Begin, 1),
                        i - _tk.ConnectionRange.Begin, _tk.BenchmarkCellConfig.GroupNameList.ToList().GetRange(i, 1),
                        _tk.Connections.Count, _tk.JobConfig.Duration, _tk.JobConfig.Interval, _tk.Counters, _brokenConnectionInds));
                }
            }

            if (_tk.BenchmarkCellConfig.EnableGroupJoinLeave) await JoinLeaveGroupOp.JoinLeaveGroup("LeaveGroup", _tk.Connections, _tk.BenchmarkCellConfig.GroupNameList.ToList(), _tk.Counters);

            await Task.WhenAll(tasks);
        }

        public override void SetCallbacks()
        {
            SetCallbacksForJoinLeaveGroup();
            SetCallbacksReceivingMessages();
        }

        protected void SetCallbacksReceivingMessages()
        {
            // set callbacks
            for (int i = _tk.ConnectionRange.Begin; i < _tk.ConnectionRange.End; i++)
            {
                _tk.ConnectionCallbacks.Add(
                    _tk.Connections[i - _tk.ConnectionRange.Begin].On("SendGroup",
                        (int count, string time, string thisId, string targetId, byte[] messageBlob) =>
                        {
                            var receiveTimestamp = Util.Timestamp();
                            var sendTimestamp = Convert.ToInt64(time);
                            var receiveSize = messageBlob != null ? messageBlob.Length * sizeof(byte) : 0;
                            _tk.Counters.CountLatency(sendTimestamp, receiveTimestamp);
                            _tk.Counters.SetServerCounter(((ulong) count));
                            _tk.Counters.IncreaseReceivedMessageSize((ulong) receiveSize);
                        }));
            }
        }

        protected void SetCallbacksForJoinLeaveGroup()
        {
            JoinLeaveGroupOp.SetCallbacks("JoinGroup", _tk.Connections, _tk.Counters);
            JoinLeaveGroupOp.SetCallbacks("LeaveGroup", _tk.Connections, _tk.Counters);
        }
    }
}