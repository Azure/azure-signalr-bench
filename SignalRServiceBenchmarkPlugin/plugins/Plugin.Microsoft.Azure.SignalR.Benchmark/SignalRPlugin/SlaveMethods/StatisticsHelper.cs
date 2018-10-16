using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Plugin.Microsoft.Azure.SignalR.Benchmark;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public static class StatisticsHelper
    {
        private static long _latencyStep = 100;
        private static long _latencyMax = 1000;

        public static void IncreaseSentMessage(ConcurrentDictionary<string, object> statistics)
        {
            statistics.AddOrUpdate(SignalRConstants.StatisticsMessageSent, (long)1, (k, v) => (long)v + 1);
        }

        public static void RecordLatency(ConcurrentDictionary<string, object> statistics, long latency)
        {
            var index = latency / _latencyStep;
            var upperBound = (index + 1) * _latencyStep;
            if (upperBound <= _latencyMax) statistics.AddOrUpdate($"message:lt:{upperBound}", (long)1, (k, v) => (long)v + 1);
            else statistics.AddOrUpdate($"message:ge:{_latencyMax}", (long)1, (k, v) => (long)v + 1);
        }
    }
}
