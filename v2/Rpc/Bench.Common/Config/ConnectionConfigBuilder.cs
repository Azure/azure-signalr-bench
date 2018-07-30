using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Bench.Common.Config
{
    public static class ThreadSafeRandom
    {
        [ThreadStatic] private static Random Local;

        public static Random ThisThreadsRandom
        {
            get { return Local ?? (Local = new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId))); }
        }
    }
    static class MyExtensions
    {
        public static void Shuffle<T>(this IList<T> list)
        {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
        }
    }
    public class ConnectionConfigBuilder
    {

        public ConnectionConfigList Build(int totalConnection, int groupNum)
        {
            var configs = new List<string>();

            for (var i = 0; i < groupNum; i++)
            {
                var count = Util.SplitNumber(totalConnection, i, groupNum);
                configs.AddRange(Enumerable.Repeat($"group_{i}", count).ToList());
            }

            Util.Log($"connection config count: {configs.Count}");
            configs.Shuffle();
            var connectionConfigList = new ConnectionConfigList();
            foreach(var groupName in configs)
            {
                connectionConfigList.Configs.Add(new ConnectionConfig {GroupName = groupName});
            }
            return connectionConfigList;
        }
    }
}