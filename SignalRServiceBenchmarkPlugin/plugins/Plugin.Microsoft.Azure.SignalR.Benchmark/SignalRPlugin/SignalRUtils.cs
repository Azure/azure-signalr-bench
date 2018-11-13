using Common;
using Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    public static class SignalRUtils
    {
        public static string GroupName(string type, int index) => $"{type}:{index}";

        public static string MessageLessThan(long latency) => $"message:lt:{latency}";

        public static string MessageGreaterOrEqaulTo(long latency) => $"message:ge:{latency}";

        public static IDictionary<string, int> MergeStatistics(IDictionary<string, object>[] results, string type, long latencyMax, long latencyStep)
        {
            var merged = new Dictionary<string, int>();

            // Sum of connection statistics
            merged[SignalRConstants.StatisticsConnectionConnectSuccess] = Sum(results, SignalRConstants.StatisticsConnectionConnectSuccess);
            merged[SignalRConstants.StatisticsConnectionConnectFail] = Sum(results, SignalRConstants.StatisticsConnectionConnectFail);

            // Sum of group statistics
            merged[SignalRConstants.StatisticsGroupJoinSuccess] = Sum(results, SignalRConstants.StatisticsGroupJoinSuccess);
            merged[SignalRConstants.StatisticsGroupJoinFail] = Sum(results, SignalRConstants.StatisticsGroupJoinFail);
            merged[SignalRConstants.StatisticsGroupLeaveSuccess] = Sum(results, SignalRConstants.StatisticsGroupLeaveSuccess);
            merged[SignalRConstants.StatisticsGroupLeaveFail] = Sum(results, SignalRConstants.StatisticsGroupLeaveFail);

            // Sum of "message:lt:latency"
            var SumMessageLatencyStatistics = (from i in Enumerable.Range(1, (int)latencyMax / (int)latencyStep)
                                               let latency = i * latencyStep
                                               select new { Key = SignalRUtils.MessageLessThan(latency), Sum = Sum(results, SignalRUtils.MessageLessThan(latency)) }).ToDictionary(entry => entry.Key, entry => entry.Sum);
            // Sum of "message:ge:latency"
            SumMessageLatencyStatistics[SignalRUtils.MessageGreaterOrEqaulTo(latencyMax)] = Sum(results, SignalRUtils.MessageGreaterOrEqaulTo(latencyMax));

            // Sum of total received message count
            merged[SignalRConstants.StatisticsMessageReceived] = SumMessageLatencyStatistics.Select(entry => entry.Value).Sum();

            // Sum of sent message statistics (should be calculated after "message:ge:latency")
            merged[SignalRConstants.StatisticsMessageSent] = Sum(results, SignalRConstants.StatisticsMessageSent);

            // Update epoch
            merged[SignalRConstants.StatisticsEpoch] = Min(results, SignalRConstants.StatisticsEpoch);

            merged = merged.Union(SumMessageLatencyStatistics).ToDictionary(entry => entry.Key, entry => entry.Value);

            return merged;
        }

        private static int Sum(IDictionary<string, object>[] results, string key)
        {
            return results.ToList().Select(statistics =>
            {
                if (statistics.ContainsKey(key))
                {
                    statistics.TryGetTypedValue(key, out int item, Convert.ToInt32);
                    return item;
                }
                return 0;
            }).Sum();
        }

        private static int Min(IDictionary<string, object>[] results, string key)
        {
            return results.ToList().Select(statistics =>
            {
                if (statistics.ContainsKey(key))
                {
                    statistics.TryGetTypedValue(key, out int item, Convert.ToInt32);
                    return item;
                }
                return 0;
            }).Min();
        }
    }
}
