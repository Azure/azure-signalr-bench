using Common;
using Microsoft.Azure.SignalR.Management;
using Plugin.Base;
using Plugin.Microsoft.Azure.SignalR.Benchmark.AgentMethods.Statistics;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.AgentMethods
{
    public class LeaveGroup : IAgentMethod
    {
        private StatisticsCollector _statisticsCollector;
        private IList<IHubConnectionAdapter> _connections;
        private List<int> _connectionIndex;
        private string _type;
        private int _groupCount;

        public async Task<IDictionary<string, object>> Do(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Leave groups...");

                // Get parameters
                stepParameters.TryGetTypedValue(SignalRConstants.Type, out _type, Convert.ToString);
                stepParameters.TryGetTypedValue(SignalRConstants.GroupCount, out _groupCount, Convert.ToInt32);
                stepParameters.TryGetTypedValue(SignalRConstants.ConnectionTotal, out int totalConnection, Convert.ToInt32);

                if (totalConnection % _groupCount != 0)
                {
                    throw new Exception("Not supported: Total connections cannot be divided by group count");
                }

                // Get context
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionStore}.{_type}",
                    out _connections, (obj) => (IList<IHubConnectionAdapter>)obj);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.StatisticsStore}.{_type}",
                    out _statisticsCollector, obj => (StatisticsCollector)obj);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionIndex}.{_type}",
                    out _connectionIndex, (obj) => (List<int>)obj);

                // Reset counters
                SignalRUtils.ResetCounters(_statisticsCollector);
                // Leave group
                var connectionType = SignalRUtils.GetClientTypeFromContext(pluginParameters, _type);
                if (connectionType == SignalREnums.ClientType.DirectConnect)
                {
                    var connectionString = SignalRUtils.FetchConnectionStringFromContext(pluginParameters, _type);
                    await DirectConnectionLeaveGroup(connectionString);
                }
                else
                {
                    await NormalConnectionLeaveGroup();
                }
                return null;
            }
            catch (Exception ex)
            {
                var message = $"Fail to leave group: {ex}";
                Log.Error(message);
                throw;
            }
        }

        private async Task NormalConnectionLeaveGroup()
        {
            if (_connections.Count >= _groupCount)
            {
                await Task.WhenAll(from i in Enumerable.Range(0, _connections.Count)
                                   select SignalRUtils.LeaveFromGroup(_connections[i],
                                                       SignalRUtils.GroupName(_type,
                                                                              _connectionIndex[i] % _groupCount),
                                                       _statisticsCollector));
            }
            else
            {
                var connectionCount = _connections.Count;
                await Task.WhenAll(from i in Enumerable.Range(0, _groupCount)
                                   select SignalRUtils.LeaveFromGroup(_connections[i % connectionCount],
                                                                      SignalRUtils.GroupName(_type, i),
                                                                      _statisticsCollector));
            }
        }

        private async Task DirectConnectionLeaveGroup(string connectionString)
        {
            var serviceManager = new ServiceManagerBuilder().WithOptions(option =>
            {
                option.ConnectionString = connectionString;
                option.ServiceTransportType = ServiceTransportType.Transient;
            }).Build();

            var hubContext = await serviceManager.CreateHubContextAsync(SignalRConstants.DefaultRestHubName);
            if (_connections.Count >= _groupCount)
            {
                for (var i = 0; i < _connections.Count; i++)
                {
                    var userId = SignalRUtils.GenClientUserIdFromConnectionIndex(_connectionIndex[i]);
                    try
                    {
                        await hubContext.UserGroups.RemoveFromGroupAsync(userId,
                            SignalRUtils.GroupName(_type, _connectionIndex[i] % _groupCount));
                        _statisticsCollector.IncreaseLeaveGroupSuccess();
                    }
                    catch (Exception e)
                    {
                        _statisticsCollector.IncreaseLeaveGroupFail();
                        Log.Error($"Fail to leave group: {e.Message}");
                    }
                }
            }
            else
            {
                for (var i = 0; i < _groupCount; i++)
                {
                    var userId = SignalRUtils.GenClientUserIdFromConnectionIndex(i);
                    try
                    {
                        await hubContext.UserGroups.RemoveFromGroupAsync(
                            userId,
                            SignalRUtils.GroupName(_type, i));
                        _statisticsCollector.IncreaseLeaveGroupSuccess();
                    }
                    catch (Exception e)
                    {
                        _statisticsCollector.IncreaseLeaveGroupFail();
                        Log.Error($"Fail to leave group: {e.Message}");
                    }
                }
            }
        }
    }
}
