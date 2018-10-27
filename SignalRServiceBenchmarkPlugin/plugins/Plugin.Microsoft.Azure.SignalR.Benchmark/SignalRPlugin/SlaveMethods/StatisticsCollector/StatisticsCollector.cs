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
        public static long LatencyStep { get; private set; } = 100;
        public static long LatencyMax { get; private set; } = 1000;

        private ConcurrentDictionary<string, long> _statistics = new ConcurrentDictionary<string, long>();

        private void ResetCounters(string containedString)
        {
            var keys = _statistics.Keys;
            var messageKeys = keys.Where(key => key.Contains(containedString));
            messageKeys.ToList().ForEach(key => _statistics.AddOrUpdate(key, 0, (k, v) => 0));
        }

        public void ResetMessageCounters()
        {
            ResetCounters("message:");
        }

        public void ResetGroupCounters()
        {
            ResetCounters("group:");
        }

        public void ResetConnectionCounters()
        {
            ResetCounters("connection:");
        }

        public void IncreaseEpoch()
        {
            _statistics.AddOrUpdate(SignalRConstants.StatisticsEpoch, 1, (k, v) => v + 1);
        }

        public void IncreaseSentMessage()
        {
            _statistics.AddOrUpdate(SignalRConstants.StatisticsMessageSent, 1, (k, v) => v + 1);
        }

        public void RecordLatency(long latency)
        {
            var index = latency / LatencyStep;
            var upperBound = (index + 1) * LatencyStep;

            if (upperBound <= LatencyMax)
            {
                _statistics.AddOrUpdate(SignalRUtils.MessageLessThan(upperBound), 1, (k, v) => v + 1);
            }
            else
            {
                _statistics.AddOrUpdate(SignalRUtils.MessageGreaterOrEqaulTo(LatencyMax), 1, (k, v) => v + 1);
            }
        }

        public IDictionary<string, object> GetData()
        {
            return _statistics.ToDictionary(entry => entry.Key, entry => (object)entry.Value);
        }

        public void IncreaseJoinGroupSuccess()
        {
            _statistics.AddOrUpdate(SignalRConstants.StatisticsGroupJoinSuccess, 1, (k, v) => v + 1);
        }

        public void IncreaseJoinGroupFail()
        {
            _statistics.AddOrUpdate(SignalRConstants.StatisticsGroupJoinFail, 1, (k, v) => v + 1);
        }

        public void IncreaseLeaveGroupSuccess()
        {
            _statistics.AddOrUpdate(SignalRConstants.StatisticsGroupLeaveSuccess, 1, (k, v) => v + 1);
        }

        public void IncreaseLeaveGroupFail()
        {
            _statistics.AddOrUpdate(SignalRConstants.StatisticsGroupLeaveFail, 1, (k, v) => v + 1);
        }

        public void IncreaseConnectionConnectSuccess()
        {
            _statistics.AddOrUpdate(SignalRConstants.StatisticsConnectionConnectSuccess, 1, (k, v) => v + 1);
        }

        public void IncreaseConnectionConnectFail()
        {
            _statistics.AddOrUpdate(SignalRConstants.StatisticsConnectionConnectFail, 1, (k, v) => v + 1);
        }
    }
}
