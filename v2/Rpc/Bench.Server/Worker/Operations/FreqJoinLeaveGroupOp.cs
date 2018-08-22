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
            await AddRemoveSendingOneToAllGroups("JoinGroup", _tk.Connections, _tk.BenchmarkCellConfig.GroupNameList.ToList().GetRange(beg, end - beg), _tk.BenchmarkCellConfig.CallbackList.ToList().GetRange(beg, end - beg));

            for (var i = _tk.ConnectionRange.Begin; i < _tk.ConnectionRange.End; i++)
            {
                var cfg = _tk.ConnectionConfigList.Configs[i];
                var ids = GenerateId("SendGroup", i - _tk.ConnectionRange.Begin);
                if (i == _tk.ConnectionRange.End - 1 && _tk.JobConfig.OneSend == 1)
                {
                    for (var j = 0; j < ids.Count && j < _tk.JobConfig.SendGroupCnt; j++)
                    {
                        tasks.Add(StartSendingMessageAsync("SendGroup", _tk.Connections[i - _tk.ConnectionRange.Begin], i - _tk.ConnectionRange.Begin, messageBlob, ids[j], _tk.Connections.Count, _tk.JobConfig.Duration, _tk.JobConfig.Interval, _tk.Counters, _brokenConnectionInds));
                    }
                }
                else if (cfg.SendFlag)
                {
                    // Util.Log($"join/leave conn {i}");
                    tasks.Add(StartJoinLeaveGroupAsync(_tk.Connections.GetRange(i - _tk.ConnectionRange.Begin, 1), i - _tk.ConnectionRange.Begin, _tk.BenchmarkCellConfig.GroupNameList.ToList().GetRange(i - _tk.ConnectionRange.Begin, 1), _tk.Connections.Count, _tk.JobConfig.Duration, _tk.JobConfig.Interval, _tk.Counters, _brokenConnectionInds));
                }
            }

            await Task.WhenAll(tasks);

            await AddRemoveSendingOneToAllGroups("LeaveGroup", _tk.Connections, _tk.BenchmarkCellConfig.GroupNameList.ToList().GetRange(beg, end - beg), _tk.BenchmarkCellConfig.CallbackList.ToList().GetRange(beg, end - beg));

        }

        private async Task AddRemoveSendingOneToAllGroups(string mode, List<HubConnection> connections, List<string> groupNameMatrix, List<bool> callbackList)
        {
            for (var i = 0; i < callbackList.Count; i++)
            {
                if (!callbackList[i]) continue;
                await JoinLeaveGroupOp.JoinLeaveGroup(mode, _tk.Connections.GetRange(i, 1),
                    groupNameMatrix.GetRange(i, 1), _tk.Counters);
            }

        }

        public override void SetCallbacks()
        {
            SetCallbacksForJoinLeaveGroup();
            SetCallbacksForSendingConnection();
        }

        protected void SetCallbacksForSendingConnection()
        {
            // set callback for the sending connection
            for (int i = _tk.ConnectionRange.Begin; i < _tk.ConnectionRange.End; i++)
            {
                if (_tk.BenchmarkCellConfig.CallbackList[i] == false) continue;

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