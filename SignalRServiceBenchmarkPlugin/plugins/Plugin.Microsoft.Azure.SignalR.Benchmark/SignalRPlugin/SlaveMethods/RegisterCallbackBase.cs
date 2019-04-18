using Common;
using Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods.Statistics;
using System;
using System.Collections.Generic;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public class RegisterCallbackBase
    {
        public static void SetCallbackOnConnected(
            IList<IHubConnectionAdapter> connections,
            StatisticsCollector statisticsCollector,
            string methodName)
        {
            foreach (var connection in connections)
            {
                connection.On(methodName, (string connectionId) =>
                {
                    connection.UpdateTimestampWhenConnected();
                });
            }
        }

        public static void SetCallback(IList<IHubConnectionAdapter> connections, StatisticsCollector statisticsCollector, string methodName)
        {
            foreach (var connection in connections)
            {
                connection.On(methodName, (IDictionary<string, object> data) =>
                {
                    var receiveTimestamp = Util.Timestamp();
                    data.TryGetTypedValue(SignalRConstants.Timestamp, out long sendTimestamp, Convert.ToInt64);
                    var latency = receiveTimestamp - sendTimestamp;
                    statisticsCollector.RecordLatency(latency);
                    SignalRUtils.RecordRecvSize(data, statisticsCollector);
                });
            }
        }

        public static void SetCallbackJoinGroup(IList<IHubConnectionAdapter> connections, StatisticsCollector statisticsCollector, string methodName=null)
        {
            foreach (var connection in connections)
            {
                connection.On(SignalRConstants.JoinGroupCallbackName, () =>
                {
                    statisticsCollector.IncreaseJoinGroupSuccess();
                });
            }
        }

        public static void SetCallbackLeaveGroup(IList<IHubConnectionAdapter> connections, StatisticsCollector statisticsCollector, string methodName = null)
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
