using Plugin.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public class SendToGroup : SendToGroupBase, ISlaveMethod
    {
        protected override Task SendMessages(IEnumerable<Package> packages)
        {
            return Task.WhenAll(from package in packages
                                let index = ConnectionIndex[package.LocalIndex]
                                let groupIndex = index % GroupCount
                                let indexInGroup = index / GroupCount
                                let connection = package.Connection
                                let data = package.Data
                                where Mode == SignalREnums.GroupConfigMode.Group
                                      && IsSending(indexInGroup, GroupInternalModulo, GroupInternalRemainderBegin, GroupInternalRemainderEnd)
                                      && IsSending(groupIndex, GroupCount, GroupLevelRemainderBegin, GroupLevelRemainderEnd)
                                      || Mode == SignalREnums.GroupConfigMode.Connection
                                      && IsSending(index, Modulo, RemainderBegin, RemainderEnd)
                                select ContinuousSend((Connection: package.Connection,
                                                       LocalIndex: package.LocalIndex,
                                                       CallbackMethod: SignalRConstants.SendToGroupCallbackName),
                                                       data,
                                                       BaseSendAsync,
                                                       TimeSpan.FromMilliseconds(Duration),
                                                       TimeSpan.FromMilliseconds(Interval),
                                                       TimeSpan.FromMilliseconds(1),
                                                       TimeSpan.FromMilliseconds(Interval)));
        }
    }
}
