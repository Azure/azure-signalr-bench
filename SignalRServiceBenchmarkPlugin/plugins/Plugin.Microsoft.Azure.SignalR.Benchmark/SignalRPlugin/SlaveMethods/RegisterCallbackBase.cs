using Common;
using Microsoft.AspNetCore.SignalR.Client;
using Plugin.Base;
using Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods.Statistics;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public class RegisterCallbackBase
    {
        public static void SetCallback(IList<HubConnection> connections, StatisticsCollector statisticsCollector, string methodName)
        {
            foreach (var connection in connections)
            {
                connection.On(methodName, (IDictionary<string, object> data) =>
                {
                    var receiveTimestamp = Util.Timestamp();
                    data.TryGetTypedValue(SignalRConstants.Timestamp, out long sendTimestamp, Convert.ToInt64);
                    var latency = receiveTimestamp - sendTimestamp;
                    statisticsCollector.RecordLatency(latency);
                });
            }
        }

        public static void SetCallbackJoinGroup(IList<HubConnection> connections, StatisticsCollector statisticsCollector, string methodName=null)
        {
            foreach (var connection in connections)
            {
                connection.On(SignalRConstants.JoinGroupCallbackName, () =>
                {
                    statisticsCollector.IncreaseJoinGroupSuccess();
                });
            }
        }

        public static void SetCallbackLeaveGroup(IList<HubConnection> connections, StatisticsCollector statisticsCollector, string methodName = null)
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
