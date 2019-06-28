using Common;
using Microsoft.Azure.SignalR.Management;
using Plugin.Base;
using Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods.Statistics;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public class JoinGroup : RegisterCallbackBase, ISlaveMethod
    {
        private StatisticsCollector _statisticsCollector;
        // connections created on the current node.
        private IList<IHubConnectionAdapter> _connections;
        private List<int> _connectionIndex;
        private string _type;
        private int _groupCount;
        // This is the total connections on all slave nodes, which may not equal to above connections number.
        private int _totalConnection;

        public async Task<IDictionary<string, object>> Do(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Join groups...");

                // Get parameters
                stepParameters.TryGetTypedValue(SignalRConstants.Type,
                    out _type, Convert.ToString);
                stepParameters.TryGetTypedValue(SignalRConstants.GroupCount,
                    out _groupCount, Convert.ToInt32);
                stepParameters.TryGetTypedValue(SignalRConstants.ConnectionTotal,
                    out _totalConnection, Convert.ToInt32);

                if (_totalConnection % _groupCount != 0)
                {
                    //throw new Exception("Not supported: Total connections cannot be divided by group count");
                    Log.Warning($"groups do not have equal members because total connections {_totalConnection} cannot be divided by group count {_groupCount}");
                }

                SignalRUtils.SaveGroupInfoToContext(pluginParameters, _type, _groupCount, _totalConnection);
                // Get context
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionStore}.{_type}",
                    out _connections, (obj) => (IList<IHubConnectionAdapter>)obj);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.StatisticsStore}.{_type}",
                    out _statisticsCollector, obj => (StatisticsCollector)obj);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionIndex}.{_type}",
                    out _connectionIndex, (obj) => (List<int>)obj);

                // Reset counters
                SignalRUtils.ResetCounters(_statisticsCollector);
                // Join group
                var connectionType = SignalRUtils.GetClientTypeFromContext(pluginParameters, _type);
                if (connectionType == SignalREnums.ClientType.DirectConnect)
                {
                    var connectionString = SignalRUtils.FetchConnectionStringFromContext(pluginParameters, _type);
                    await DirectConnectionJoinGroup(connectionString);
                }
                else
                {
                    await NormalConnectionJoinGroup();
                }
                return null;
            }
            catch (Exception ex)
            {
                var message = $"Fail to join group: {ex}";
                Log.Error(message);
                throw;
            }
        }

        private async Task DirectConnectionJoinGroup(string connectionString)
        {
            var serviceManager = new ServiceManagerBuilder().WithOptions(option =>
            {
                option.ConnectionString = connectionString;
                option.ServiceTransportType = ServiceTransportType.Transient;
            }).Build();

            var hubContext = await serviceManager.CreateHubContextAsync(SignalRConstants.DefaultRestHubName);
            await SignalRUtils.JoinGroupForConnection(
                _totalConnection,
                _groupCount,
                _connectionIndex,
                async (i, g) =>
                {
                    var userId = SignalRUtils.GenClientUserIdFromConnectionIndex(_connectionIndex[i]);
                    try
                    {
                        await hubContext.UserGroups.AddToGroupAsync(
                            userId,
                            SignalRUtils.GroupName(_type, g));
                        _statisticsCollector.IncreaseJoinGroupSuccess();
                    }
                    catch (Exception e)
                    {
                        _statisticsCollector.IncreaseJoinGroupFail();
                        Log.Error($"Fail to join group: {e.Message}");
                    }
                });
        }

        private async Task NormalConnectionJoinGroup()
        {
            await SignalRUtils.JoinGroupForConnection(
                _totalConnection,
                _groupCount,
                _connectionIndex,
                async (i, g) =>
            {
                await SignalRUtils.JoinToGroup(
                        _connections[i],
                        SignalRUtils.GroupName(_type, g),
                        _statisticsCollector);
            });
        }
    }
}
