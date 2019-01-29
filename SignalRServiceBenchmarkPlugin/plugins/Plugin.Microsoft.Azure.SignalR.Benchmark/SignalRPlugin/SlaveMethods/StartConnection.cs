using Common;
using Plugin.Base;
using Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods.Statistics;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public class StartConnection : ISlaveMethod
    {
        private StatisticsCollector _statisticsCollector;

        public async Task<IDictionary<string, object>> Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Start connections...");

                // Get parameters
                stepParameters.TryGetTypedValue(SignalRConstants.ConcurrentConnection,
                    out int concurrentConnection, Convert.ToInt32);
                stepParameters.TryGetTypedValue(SignalRConstants.Type,
                    out string type, Convert.ToString);
                // Default use high pressure batch mode
                SignalRUtils.TryGetBatchMode(
                    stepParameters,
                    out string batchConfigMode,
                    out int batchWaitMilliSeconds,
                    out SignalREnums.BatchMode mode);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionStore}.{type}",
                    out IList<IHubConnectionAdapter> connections, (obj) => (IList<IHubConnectionAdapter>)obj);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.StatisticsStore}.{type}",
                    out _statisticsCollector, obj => (StatisticsCollector)obj);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionSuccessFlag}.{type}",
                    out List<SignalREnums.ConnectionState> connectionsSuccessFlag,
                    (obj) => (List<SignalREnums.ConnectionState>)obj);

                // The following get connection Id needs the concurrent connection value
                pluginParameters.TryAdd(SignalRConstants.ConcurrentConnection, concurrentConnection);

                var packages = (from i in Enumerable.Range(0, connections.Count())
                                select (Connection: connections[i], LocalIndex: i,
                                ConnectionsSuccessFlag: connectionsSuccessFlag,
                                NormalState: SignalREnums.ConnectionState.Success,
                                AbnormalState: SignalREnums.ConnectionState.Fail)).ToList();
                switch (mode)
                {
                    case SignalREnums.BatchMode.LimitRatePress:
                        var duration = SignalRConstants.RateLimitDefaultGranularity; // 100 milli-seconds is the default fine-grind
                        var fillTokenPerDuration = concurrentConnection > duration ? concurrentConnection / duration : 1;
                        await Task.WhenAll(Util.RateLimitBatchProces(packages,
                            SignalRUtils.StartConnect, concurrentConnection, fillTokenPerDuration, duration));
                        break;
                    case SignalREnums.BatchMode.HighPress:
                        await Task.WhenAll(Util.BatchProcess(packages,
                            SignalRUtils.StartConnect, concurrentConnection));
                        break;
                    case SignalREnums.BatchMode.LowPress:
                        await Task.WhenAll(Util.LowPressBatchProcess(packages,
                            SignalRUtils.StartConnect, concurrentConnection, batchWaitMilliSeconds));
                        break;
                }
                return null;
            }
            catch (Exception ex)
            {
                var message = $"Fail to start connections: {ex}";
                Log.Error(message);
                throw;
            }
        }
    }
}
