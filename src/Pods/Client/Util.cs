using Azure.SignalRBench.Common;
using System;
using System.Linq;

namespace Azure.SignalRBench.Client
{
    internal static class Util
    {
        public static int[] GenerateIndexMap(int total, int startIndex, int count)
        {
            var rand = new Random(total);
            return (from id in Enumerable.Range(0, total)
                    orderby rand.Next()
                    select id).Skip(startIndex).Take(count).ToArray();
        }

        public static string GenerateRandomData(int size)
        {
            var message = new byte[size * 3 / 4 + 1];
            StaticRandom.NextBytes(message);
            return Convert.ToBase64String(message).Substring(0, size);
        }
    }
}
