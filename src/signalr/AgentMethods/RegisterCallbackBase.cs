using Common;
using Plugin.Microsoft.Azure.SignalR.Benchmark.AgentMethods.Statistics;
using Serilog;
using System;
using System.Collections.Generic;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.AgentMethods
{
    public class RegisterCallbackBase
    {
        public static void SetCallbackOnConnected(
            IList<IHubConnectionAdapter> connections,
            StatisticsCollector statisticsCollector)
        {
            foreach (var connection in connections)
            {
                connection.On(SignalRConstants.OnConnectedCallback, (string connectionId) =>
                {
                    connection.UpdateTimestampWhenConnected();
                    connection.ConnectionId = connectionId;
                });
            }
        }

        public static void SetDummyLatencyCallback(
            IList<IHubConnectionAdapter> connections,
            StatisticsCollector statisticsCollector)
        {
            foreach (var connection in connections)
            {
                connection.On(SignalRConstants.RecordLatencyCallbackName, (IDictionary<string, object> data) =>
                {
                    //var receiveTimestamp = Util.Timestamp();
                    //data.TryGetTypedValue(SignalRConstants.Timestamp, out long sendTimestamp, Convert.ToInt64);
                    //var latency = receiveTimestamp - sendTimestamp;
                    //statisticsCollector.RecordLatency(latency);
                    SignalRUtils.RecordRecvSize(data, statisticsCollector);
                });
            }
        }

        public static void SetCallback(
            IList<IHubConnectionAdapter> connections,
            StatisticsCollector statisticsCollector)
        {
            foreach (var connection in connections)
            {
                connection.On(SignalRConstants.RecordLatencyCallbackName, (IDictionary<string, object> data) =>
                {
                    var receiveTimestamp = Util.Timestamp();
                    data.TryGetTypedValue(SignalRConstants.Timestamp, out long sendTimestamp, Convert.ToInt64);
                    var latency = receiveTimestamp - sendTimestamp;
                    if (!statisticsCollector.RecordLatency(latency))
                    {
                        connection.BigMessageLatencyCount = connection.BigMessageLatencyCount + 1;
                    }
                    SignalRUtils.RecordRecvSize(data, statisticsCollector);
                });
            }
        }

        public static void SetCallbackJoinGroup(
            IList<IHubConnectionAdapter> connections,
            StatisticsCollector statisticsCollector,
            string methodName=null)
        {
            foreach (var connection in connections)
            {
                connection.On(SignalRConstants.JoinGroupCallbackName, () =>
                {
                    statisticsCollector.IncreaseJoinGroupSuccess();
                });
            }
        }

        public static void SetCallbackLeaveGroup(
            IList<IHubConnectionAdapter> connections,
            StatisticsCollector statisticsCollector)
        {
            foreach (var connection in connections)
            {
                connection.On(SignalRConstants.LeaveGroupCallbackName, () =>
                {
                    statisticsCollector.IncreaseLeaveGroupSuccess();
                });
            }
        }
    }
}
