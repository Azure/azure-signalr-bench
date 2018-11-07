using Common;
using Microsoft.AspNetCore.SignalR.Client;
using Plugin.Base;
using Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods.Statistics;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public class JoinGroup : ISlaveMethod
    {
        private StatisticsCollector _statisticsCollector;

        public async Task<IDictionary<string, object>> Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Join groups...");

                // Get parameters
                stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
                stepParameters.TryGetTypedValue(SignalRConstants.GroupCount, out int groupCount, Convert.ToInt32);
                stepParameters.TryGetTypedValue(SignalRConstants.ConnectionTotal, out int totalConnection, Convert.ToInt32);

                if (totalConnection % groupCount != 0) throw new Exception("Not supported: Total connections cannot be divided by group count");

                // Get context
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionStore}.{type}", out IList<HubConnection> connections, (obj) => (IList<HubConnection>)obj);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionOffset}.{type}", out int offset, Convert.ToInt32);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.StatisticsStore}.{type}", out _statisticsCollector, obj => (StatisticsCollector)obj);

                // Reset counters
                _statisticsCollector.ResetGroupCounters();
                _statisticsCollector.ResetMessageCounters();

                // Join group
                await Task.WhenAll(from i in Enumerable.Range(0, connections.Count)
                                   select JoinIntoGroup(connections[i], SignalRUtils.GroupName(type, (i + offset) % groupCount)));

                return null;
            }
            catch (Exception ex)
            {
                var message = $"Fail to join group: {ex}";
                Log.Error(message);
                throw;
            }
        }

        private async Task JoinIntoGroup(HubConnection connection, string groupName)
        {
            try
            {
                await connection.SendAsync(SignalRConstants.JoinGroupCallbackName, groupName);
            }
            catch
            {
                _statisticsCollector.IncreaseJoinGroupFail();
            }
        }

    }
}
