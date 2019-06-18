using Common;
using Plugin.Base;
using Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods.Statistics;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    class RepairConnections : ISlaveMethod
    {
        private int _concurrentConnection;
        private SignalREnums.ActionAfterConnection _action = SignalREnums.ActionAfterConnection.None;
        private string _type;
        private IDictionary<string, object> _context;

        public Task<IDictionary<string, object>> Do(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters)
        {
            stepParameters.TryGetTypedValue(SignalRConstants.Type,
                out string type, Convert.ToString);
            pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionStore}.{type}",
                out IList<IHubConnectionAdapter> connections, (obj) => (IList<IHubConnectionAdapter>)obj);

            if (stepParameters.TryGetValue(SignalRConstants.ActionAfterConnect, out _))
            {
                stepParameters.TryGetTypedValue(SignalRConstants.ActionAfterConnect,
                    out string postAction, Convert.ToString);
                if (Enum.TryParse(postAction, out SignalREnums.ActionAfterConnection action))
                    _action = action;
            }
            _concurrentConnection =
                SignalRUtils.FetchConcurrentConnectionCountFromContext(
                    pluginParameters,
                    type,
                    connections.Count);
            _type = type;

            // register the CTS to control the connector repairer.
            var cts = new CancellationTokenSource();
            pluginParameters[$"{SignalRConstants.RepairConnectionCTS}.{type}"] = cts;
            _context = pluginParameters;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Start(connections, cts);
                }
                catch (Exception e)
                {
                    Log.Error($"Fail to repair the connection: {e.Message}");
                }
            });
            return Task.FromResult<IDictionary<string, object>>(null); ;
        }

        private void UpdateReconnect(int count)
        {
            _context.TryGetTypedValue($"{SignalRConstants.StatisticsStore}.{_type}",
                            out StatisticsCollector statisticsCollector, (obj) => (StatisticsCollector)obj);
            statisticsCollector.UpdateReconnect(count);
        }

        private async Task NonePostAction(IList<IHubConnectionAdapter> connections)
        {
            var packages = (from i in Enumerable.Range(0, connections.Count())
                            where connections[i].GetStat() != SignalREnums.ConnectionInternalStat.Active
                            select (Connection: connections[i], LocalIndex: i)).ToList();
            Log.Information($"Waiting for dropped {packages.Count} connections recover");
            await Util.BatchProcess(packages, SignalRUtils.StartConnect, _concurrentConnection);
            var recoverred = (from i in Enumerable.Range(0, packages.Count)
                              where packages[i].Connection.GetStat() == SignalREnums.ConnectionInternalStat.Active
                              select packages[i].Connection).ToList();
            Log.Information($"Dropped {packages.Count} connections have finished, and {recoverred.Count} connections are recoverred");
            UpdateReconnect(recoverred.Count);
        }

        private async Task PostActionAfterReconnect(IList<IHubConnectionAdapter> connections)
        {
            var packages = (from i in Enumerable.Range(0, connections.Count())
                            where connections[i].GetStat() != SignalREnums.ConnectionInternalStat.Active
                            select (Connection: connections[i],
                                    LocalIndex: i,
                                    Action: _action,
                                    Context: _context,
                                    Type: _type)).ToList();
            Log.Information($"Waiting for dropped {packages.Count} connections recover");
            await Util.BatchProcess(
                packages,
                SignalRUtils.TakeActionAfterStartingConnect,
                _concurrentConnection);
            Log.Information($"Dropped {packages.Count} connections have recovered");
            var recoverred = (from i in Enumerable.Range(0, packages.Count)
                              where packages[i].Connection.GetStat() == SignalREnums.ConnectionInternalStat.Active
                              select packages[i].Connection).ToList();
            Log.Information($"Dropped {packages.Count} connections have finished, and {recoverred.Count} connections are recoverred");
            UpdateReconnect(recoverred.Count);
        }

        private async Task Start(
            IList<IHubConnectionAdapter> connections,
            CancellationTokenSource cts)
        {
            Log.Information($"Launch the {connections.Count} connections repair process");
            while (!cts.IsCancellationRequested)
            {
                var droppedConnectionIdxList = (from i in Enumerable.Range(0, connections.Count())
                                where connections[i].GetStat() != SignalREnums.ConnectionInternalStat.Active
                                select i).ToList();
                if (droppedConnectionIdxList.Count > 0)
                {
                    if (_action == SignalREnums.ActionAfterConnection.None)
                    {
                        await NonePostAction(connections);
                    }
                    else
                    {
                        await PostActionAfterReconnect(connections);
                    }
                }
                else
                {
                    Log.Information($"All connections are active");
                }
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
            Log.Information("The connection repair process ends");
        }
    }
}
