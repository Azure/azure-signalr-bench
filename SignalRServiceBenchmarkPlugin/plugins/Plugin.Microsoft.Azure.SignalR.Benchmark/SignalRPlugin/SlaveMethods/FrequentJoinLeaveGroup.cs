using Common;
using Microsoft.AspNetCore.SignalR.Client;
using Plugin.Base;
using Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods.Statistics;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public class FrequentJoinLeaveGroup : SendToGroupBase, ISlaveMethod
    {
        protected override Task SendMessages(IEnumerable<Package> packages)
        {
            // Send messages
            return Task.WhenAll(from package in packages
                               let index = connectionIndex[package.LocalIndex]
                               let groupSize = totalConnection / groupCount
                               let groupIndex = index % groupCount
                               let indexInGroup = index / groupCount
                               let connection = package.Connection
                               let data = package.Data
                               let isSendGroupLevel = IsSending(groupIndex, groupCount, GroupLevelRemainderBegin, GroupLevelRemainderEnd)
                               let isSendGroupInternal = IsSending(indexInGroup, GroupInternalModulo, GroupInternalRemainderBegin, GroupInternalRemainderEnd)
                               select GenerateTask((Connection: connections[package.LocalIndex], LocalIndex: package.LocalIndex, ConnectionsSuccessFlag: connectionsSuccessFlag, StatisticsCollector: statisticsCollector), data, isSendGroupLevel, isSendGroupInternal));

        }

        protected override IEnumerable<Package> GenerateData()
        {
            // Generate necessary data
            var messageBlob = new byte[messageSize];

            var packages = from i in Enumerable.Range(0, connections.Count)
                           let groupName = SignalRUtils.GroupName(type, i % groupCount)
                           select
                           new Package
                           {
                               LocalIndex = i,
                               Connection = connections[i],
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

        private Task GenerateTask((HubConnection Connection, int LocalIndex, List<SignalREnums.ConnectionState> ConnectionsSuccessFlag, StatisticsCollector StatisticsCollector) connection, IDictionary<string, object> data, bool isSendGroupLevel, bool isSendGroupInternal)
        {
            if (isSendGroupLevel && isSendGroupInternal)
            {
                return ContinuousSend(connection, data, SendGroup,
                                TimeSpan.FromMilliseconds(duration), TimeSpan.FromMilliseconds(interval),
                                TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(interval));
            }
            else if (isSendGroupLevel && !isSendGroupInternal)
            {
                return ContinuousSend(connection, data, JoinLeaveGroup,
                                TimeSpan.FromMilliseconds(duration), TimeSpan.FromMilliseconds(interval),
                                TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(interval));
            }
            return Task.CompletedTask;
        }
    }
}
