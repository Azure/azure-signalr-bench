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
    public class SendToGroup : SendToGroupBase, ISlaveMethod
    {
        protected override Task SendMessages(IEnumerable<Package> packages)
        {
            return Task.WhenAll(from package in packages
                                let index = connectionIndex[package.LocalIndex]
                                let groupSize = totalConnection / groupCount
                                let groupIndex = index % groupCount
                                let indexInGroup = index / groupCount
                                let connection = package.Connection
                                let data = package.Data
                                where IsSending(indexInGroup, GroupInternalModulo, GroupInternalRemainderBegin, GroupInternalRemainderEnd) &&
                                      IsSending(groupIndex, groupCount, GroupLevelRemainderBegin, GroupLevelRemainderEnd)
                                select ContinuousSend((Connection: connections[package.LocalIndex], LocalIndex: package.LocalIndex, ConnectionsSuccessFlag: connectionsSuccessFlag, StatisticsCollector: statisticsCollector), data, SendGroup,
                                     TimeSpan.FromMilliseconds(duration), TimeSpan.FromMilliseconds(interval),
                                     TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(interval)));
        }
    }
}
