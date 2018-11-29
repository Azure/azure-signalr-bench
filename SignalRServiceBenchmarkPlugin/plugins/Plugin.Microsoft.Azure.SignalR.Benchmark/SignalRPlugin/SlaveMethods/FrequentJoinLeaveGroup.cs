using Plugin.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public class FrequentJoinLeaveGroup : SendToGroupBase, ISlaveMethod
    {
        protected override Task SendMessages(IEnumerable<Package> packages)
        {
            // Send messages
            return Task.WhenAll(from package in packages
                                let index = ConnectionIndex[package.LocalIndex]
                                let groupSize = TotalConnection / GroupCount
                                let groupIndex = index % GroupCount
                                let indexInGroup = index / GroupCount
                                let connection = package.Connection
                                let data = package.Data
                                let isSendGroupLevel = Mode == SignalREnums.GroupConfigMode.Group &&
                                    IsSending(groupIndex, GroupCount, GroupLevelRemainderBegin, GroupLevelRemainderEnd)
                                let isSendGroupInternal = Mode == SignalREnums.GroupConfigMode.Group &&
                                    IsSending(indexInGroup, GroupInternalModulo, GroupInternalRemainderBegin, GroupInternalRemainderEnd)
                                let isSendConnectionMode = Mode == SignalREnums.GroupConfigMode.Connection &&
                                    IsSending(index, Modulo, RemainderBegin, RemainderEnd)
                                select Mode == SignalREnums.GroupConfigMode.Group ? 
                                GenerateTaskForGroupMode(package.LocalIndex, data, isSendGroupLevel, isSendGroupInternal) : GenerateTaskForGroupMode(package.LocalIndex, data, true, isSendConnectionMode));

        }

        protected override IEnumerable<Package> GenerateData()
        {
            // Generate necessary data
            var messageBlob = new byte[MessageSize];

            var packages = from i in Enumerable.Range(0, Connections.Count)
                           let groupName = SignalRUtils.GroupName(Type, i % GroupCount)
                           select
                           new Package
                           {
                               LocalIndex = i,
                               Connection = Connections[i],
                               GroupName = groupName,
                               Data = new Dictionary<string, object>
                                   {
                                       { SignalRConstants.MessageBlob, messageBlob }, // message payload
                                       { SignalRConstants.GroupName, groupName},
                                       { _isIngroup, false}
                                  }
                           };

            return packages;
        }

        private Task GenerateTaskForGroupMode(int localIndex, IDictionary<string, object> data, bool isSendGroupLevel, bool isSendGroupInternal)
        {
            if (isSendGroupLevel && isSendGroupInternal)
            {
                return ContinuousSend(localIndex, data, SendGroup,
                                TimeSpan.FromMilliseconds(Duration), TimeSpan.FromMilliseconds(Interval),
                                TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(Interval));
            }
            else if (isSendGroupLevel && !isSendGroupInternal)
            {
                return ContinuousSend(localIndex, data, JoinLeaveGroup,
                                TimeSpan.FromMilliseconds(Duration), TimeSpan.FromMilliseconds(Interval),
                                TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(Interval));
            }
            return Task.CompletedTask;
        }
    }
}
