using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plugin.Microsoft.Azure.SignalR.Benchmark;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods.Statistics
{
    public class StatisticsCollector
    {
        private static readonly long _latencyStep = 100;
        private static readonly long _latencyMax = 1000;

        private ConcurrentDictionary<string, long> _statistics = new ConcurrentDictionary<string, long>();

        public void IncreaseSentMessage()
        {
            _statistics.AddOrUpdate(SignalRConstants.StatisticsMessageSent, 1, (k, v) => v + 1);
        }

        public void RecordLatency(long latency)
        {
            var index = latency / _latencyStep;
            var upperBound = (index + 1) * _latencyStep;

            if (upperBound <= _latencyMax)
            {
                _statistics.AddOrUpdate($"message:lt:{upperBound}", 1, (k, v) => v + 1);
            }
            else
            {
                _statistics.AddOrUpdate($"message:ge:{_latencyMax}", 1, (k, v) => v + 1);
            }
        }

        public IDictionary<string, object> GetData()
        {
            return _statistics.ToDictionary(entry => entry.Key, entry => (object)entry.Value);
        }
    }
}
