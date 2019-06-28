using Common;
using Plugin.Microsoft.Azure.SignalR.Benchmark;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.SignalR.Benchmark.Tests
{
    public class TestSignalRUtils
    {
        // write something to console:
        //   _output.WriteLine($"Token{i}: {tok}");
        //   dotnet test --logger:"console;verbosity=detailed"
        private readonly ITestOutputHelper _output;

        public TestSignalRUtils(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestTryGetBatchMode()
        {
            var dic = new Dictionary<string, object>();
            SignalRUtils.TryGetBatchMode(
                dic,
                out string batchConfigMode,
                out int batchWaitMilliSeconds,
                out SignalREnums.BatchMode mode);
            Assert.Equal(batchConfigMode, SignalRConstants.DefaultBatchMode);
        }

        [Fact]
        public async Task TestJoinGroupForConnection1()
        {
            int total = 6, group = 15, slaves = 2;
            // we expect every connection joins two or three different groups
            var indexes = Enumerable.Range(0, total).ToList();
            indexes.Shuffle();
            var expectedGroup = Enumerable.Range(0, group).ToList();
            var assignedGroup = new List<int>();
            var connectionGroupDic = new Dictionary<int, List<int>>();
            for (var i = 0; i < slaves; i++)
            {
                (int beg, int end) = Util.GetConnectionRange(total, i, slaves);
                var connectIndex = indexes.GetRange(beg, end - beg);

                await SignalRUtils.JoinGroupForConnection(total, group, connectIndex, (index, groupIndex) =>
                {
                    //_output.WriteLine($"{connectIndex[index]}: {groupIndex}");
                    if (!connectionGroupDic.TryGetValue(connectIndex[index], out _))
                    {
                        connectionGroupDic[connectIndex[index]] = new List<int>();
                    }
                    connectionGroupDic[connectIndex[index]].Add(groupIndex);
                    return Task.CompletedTask;
                });
            }
            Assert.Equal(connectionGroupDic.Keys.Count, total);
            int groupCount = 0;
            foreach (var key in connectionGroupDic.Keys)
            {
                groupCount += connectionGroupDic[key].Count;
                assignedGroup.AddRange(connectionGroupDic[key]);
            }
            Assert.Equal(groupCount, group);
            assignedGroup.Sort();
            var diff = expectedGroup.Except(assignedGroup).ToList();
            Assert.Empty(diff);
        }

        [Theory]
        [InlineData(6, 12, 2)]
        [InlineData(6, 6, 2)]
        public async Task TestJoinGroupForConnection2(int total, int group, int slaves)
        {
            //int total = 6, group = 12, slaves = 2;
            // we expect every connection joins two different groups
            var indexes = Enumerable.Range(0, total).ToList();
            indexes.Shuffle();
            var expectedGroup = Enumerable.Range(0, group).ToList();
            var assignedGroup = new List<int>();
            var connectionGroupDic = new Dictionary<int, List<int>>();
            for (var i = 0; i < slaves; i++)
            {
                (int beg, int end) = Util.GetConnectionRange(total, i, slaves);
                var connectIndex = indexes.GetRange(beg, end - beg);

                await SignalRUtils.JoinGroupForConnection(total, group, connectIndex, (index, groupIndex) =>
                {
                    //_output.WriteLine($"{connectIndex[index]}: {groupIndex}");
                    if (!connectionGroupDic.TryGetValue(connectIndex[index], out _))
                    {
                        connectionGroupDic[connectIndex[index]] = new List<int>();
                    }
                    connectionGroupDic[connectIndex[index]].Add(groupIndex);
                    return Task.CompletedTask;
                });
            }
            Assert.Equal(connectionGroupDic.Keys.Count, total);
            foreach (var key in connectionGroupDic.Keys)
            {
                Assert.Equal(group/total, connectionGroupDic[key].Count);
                assignedGroup.AddRange(connectionGroupDic[key]);
            }
            assignedGroup.Sort();
            var diff = expectedGroup.Except(assignedGroup).ToList();
            Assert.Empty(diff);
        }

        [Fact]
        public async Task TestJoinGroupForConnection3()
        {
            int total = 6, group = 3, slaves = 2;
            // we expect every 2 connections join one groups
            var indexes = Enumerable.Range(0, total).ToList();
            indexes.Shuffle();
            var assignedConnect = new List<int>();
            var connectionGroupDic = new Dictionary<int, List<int>>();
            for (var i = 0; i < slaves; i++)
            {
                (int beg, int end) = Util.GetConnectionRange(total, i, slaves);
                var connectIndex = indexes.GetRange(beg, end - beg);

                await SignalRUtils.JoinGroupForConnection(total, group, connectIndex, (index, groupIndex) =>
                {
                    if (!connectionGroupDic.TryGetValue(groupIndex, out _))
                    {
                        connectionGroupDic[groupIndex] = new List<int>();
                    }
                    connectionGroupDic[groupIndex].Add(connectIndex[index]);
                    return Task.CompletedTask;
                });
            }
            Assert.Equal(connectionGroupDic.Keys.Count, group);
            foreach (var key in connectionGroupDic.Keys)
            {
                Assert.Equal(total / group, connectionGroupDic[key].Count);
                assignedConnect.AddRange(connectionGroupDic[key]);
            }
            assignedConnect.Sort();
            var diff = indexes.Except(assignedConnect).ToList();
            Assert.Empty(diff);
        }
    }
}
