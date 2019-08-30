using Common;
using Serilog;
using System.Collections.Generic;
using System.Linq;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.AgentMethods.Statistics
{
    public class ConnectionStatisticCollector : StatisticsCollector
    {
        private IList<IHubConnectionAdapter> _connections;

        public ConnectionStatisticCollector(
            IList<IHubConnectionAdapter> connections,
            long latencyStep = SignalRConstants.LATENCY_STEP,
            long latencyMax = SignalRConstants.LATENCY_MAX)
            : base(SignalRConstants.LATENCY_STEP, SignalRConstants.LATENCY_MAX)
        {
            _connections = connections;
        }

        // Get all statistics including connection statistic and message latency statistic
        public override IDictionary<string, object> GetData()
        {
            var data = base.GetData();
            AddConnectionStat(data);
            AddBigLatencyCount(data);
            return data;
        }

        private void AddBigLatencyCount(IDictionary<string, object> data)
        {
            long bigLatencyCount = 0;
            int maxIndex = -1;
            for (var i = 0; i < _connections.Count; i++)
            {
                if (_connections[i].BigMessageLatencyCount > bigLatencyCount)
                {
                    bigLatencyCount = _connections[i].BigMessageLatencyCount;
                    maxIndex = i;
                }
            }
            if (bigLatencyCount > 0)
            {
                Log.Information($"Max latency count: {bigLatencyCount}, ConnectionID: {_connections[maxIndex].ConnectionId}");
            }
        }

        private void AddConnectionStat(IDictionary<string, object> data)
        {
            var lifeSpanArray = new int[_connections.Count];
            var connectionCostArray = new int[_connections.Count];
            var reconnectCostArray = new int[_connections.Count];
            var connectionSLArray = new int[_connections.Count];
            var offlineArray = new int[_connections.Count];
            for (var i = 0; i < _connections.Count; i++)
            {
                var connection = _connections[i];
                if (connection.GetStat() == SignalREnums.ConnectionInternalStat.Active)
                {
                    var now = Util.Timestamp();
                    var connectionBornTimestamp = connection.ConnectionBornTimestamp;
                    var connectedTimestamp = connection.ConnectedTimestamp;
                    var downTimePeriod = connection.DowntimePeriod;
                    var lastDisconnectingTimestamp = connection.LastDisconnectedTimestamp;
                    var startConnectingTimestamp = connection.StartConnectingTimestamp;

                    if (connectionBornTimestamp != -1)
                    {
                        lifeSpanArray[i] = (int)(now - connectionBornTimestamp);
                    }
                    if (startConnectingTimestamp != -1 && connectedTimestamp != -1)
                    {
                        connectionCostArray[i] = (int)(connectedTimestamp - startConnectingTimestamp);
                    }
                    if (lastDisconnectingTimestamp != -1 && connectedTimestamp != -1)
                    {
                        reconnectCostArray[i] = (int)(connectedTimestamp - lastDisconnectingTimestamp);
                    }
                    if (downTimePeriod > 0)
                    {
                        offlineArray[i] = (int)downTimePeriod;
                    }
                    if (lifeSpanArray[i] > 0)
                    {
                        double sla = 100.0;
                        if (downTimePeriod > 0)
                        {
                            sla = (lifeSpanArray[i] - downTimePeriod) * 100.0 / lifeSpanArray[i];
                        }
                        connectionSLArray[i] = (int)(sla);
                    }
                }
            }
            data[SignalRConstants.StatisticsConnectionLifeSpan] = string.Join(',', lifeSpanArray);
            data[SignalRConstants.StatisticsConnectionCost] = string.Join(',', connectionCostArray);
            data[SignalRConstants.StatisticsConnectionReconnectCost] = string.Join(',', reconnectCostArray);
            data[SignalRConstants.StatisticsConnectionSLA] = string.Join(',', connectionSLArray);
            data[SignalRConstants.StatisticsConnectionOfflinetime] = string.Join(',', offlineArray);
        }
    }
}
