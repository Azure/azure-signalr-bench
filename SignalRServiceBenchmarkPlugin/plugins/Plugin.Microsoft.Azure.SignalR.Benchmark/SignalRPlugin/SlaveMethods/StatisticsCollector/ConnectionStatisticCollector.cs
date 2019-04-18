using Common;
using Serilog;
using System.Collections.Generic;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods.Statistics
{
    public class ConnectionStatisticCollector : StatisticsCollector
    {
        private IList<IHubConnectionAdapter> _connections;

        public ConnectionStatisticCollector(
            IList<IHubConnectionAdapter> connections,
            long latencyStep = LATENCY_STEP,
            long latencyMax = LATENCY_MAX)
            : base(LatencyStep, LatencyMax)
        {
            _connections = connections;
        }

        // Get all statistics including connection statistic and message latency statistic
        public override IDictionary<string, object> GetData()
        {
            var data = base.GetData();
            AddConnectionStat(data);
            return data;
        }

        private void AddConnectionStat(IDictionary<string, object> data)
        {
            var lifeSpanArray = new int[_connections.Count];
            var connectionCostArray = new int[_connections.Count];
            var reconnectCostArray = new int[_connections.Count];
            var connectionSLArray = new int[_connections.Count];
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
        }
    }
}
