using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods.Statistics
{
    public class StatisticsCollector
    {
        public long LatencyStep;
        public long LatencyMax;

        protected ConcurrentDictionary<string, long> _statistics = new ConcurrentDictionary<string, long>();

        protected object _lock = new object();

        public StatisticsCollector(long latencyStep, long latencyMax)
        {
            LatencyStep = latencyStep;
            LatencyMax = latencyMax;
        }

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

        public void ResetReconnectCounters()
        {
            ResetCounters(SignalRConstants.StatisticsConnectionReconnect);
        }

        public void SetSendingStep(long sendingStep)
        {
            _statistics.AddOrUpdate(SignalRConstants.StatisticsSendingStep,
                sendingStep, (k, v) => sendingStep);
        }

        public void IncreaseEpoch()
        {
            _statistics.AddOrUpdate(SignalRConstants.StatisticsEpoch, 1, (k, v) => v + 1);
        }

        public void IncreaseSentMessage()
        {
            _statistics.AddOrUpdate(SignalRConstants.StatisticsMessageSent, 1, (k, v) => v + 1);
        }

        public void IncreaseSendSize(long sz)
        {
            _statistics.AddOrUpdate(SignalRConstants.StatisticsMessageSentSize, sz, (k, v) => v + sz);
        }

        public void IncreaseRecvSize(long sz)
        {
            _statistics.AddOrUpdate(SignalRConstants.StatisticsMessageReceivedSize, sz, (k, v) => v + sz);
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
                _statistics.AddOrUpdate(SignalRUtils.MessageGreaterOrEqualTo(LatencyMax), 1, (k, v) => v + 1);
            }
        }

        public virtual IDictionary<string, object> GetData()
        {
            lock(_lock)
            {
                return _statistics.ToDictionary(entry => entry.Key, entry => (object)entry.Value);
            }
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

        public void UpdateReconnect(int reconnectCount)
        {
            lock (_lock)
            {
                _statistics.AddOrUpdate(
                SignalRConstants.StatisticsConnectionReconnect,
                reconnectCount, (k, v) => v + reconnectCount);
            }
        }

        public void UpdateConnectionsInternalStat(IList<IHubConnectionAdapter> connections)
        {
            var success = 0;
            var fail = 0;
            var init = 0;
            foreach (var connection in connections)
            {
                switch (connection.GetStat())
                {
                    case SignalREnums.ConnectionInternalStat.Active:
                        success++;
                        break;
                    case SignalREnums.ConnectionInternalStat.Stopped:
                    case SignalREnums.ConnectionInternalStat.Disposed:
                        fail++;
                        break;
                    case SignalREnums.ConnectionInternalStat.Init:
                        init++;
                        break;
                }
            }
            lock (_lock)
            {
                // update them together to avoid unconsistency
                _statistics.AddOrUpdate(SignalRConstants.StatisticsConnectionConnectSuccess, success, (k, v) => success);
                _statistics.AddOrUpdate(SignalRConstants.StatisticsConnectionConnectFail, fail, (k, v) => fail);
                _statistics.AddOrUpdate(SignalRConstants.StatisticsConnectionInit, init, (k, v) => init);
            }
        }
    }
}
