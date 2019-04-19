using Common;
using Plugin.Base;
using Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods.Statistics;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public class StartConnection : BatchConnectionBase, ISlaveMethod
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
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionStore}.{type}",
                    out IList<IHubConnectionAdapter> connections, (obj) => (IList<IHubConnectionAdapter>)obj);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.StatisticsStore}.{type}",
                    out _statisticsCollector, obj => (StatisticsCollector)obj);
                SignalRUtils.FilterOnConnectedNotification(pluginParameters, type);
                // The following get connection Id needs the concurrent connection value
                SignalRUtils.SaveConcurrentConnectionCountToContext(
                    pluginParameters,
                    type,
                    concurrentConnection);

                var sw = new Stopwatch();
                sw.Start();
                Log.Information($"{DateTime.Now.ToString("yyyyMMddHHmmss")} Start connection");
                try
                {
                    await BatchConnect(
                        stepParameters,
                        pluginParameters,
                        connections,
                        concurrentConnection);
                }
                finally
                {
                    sw.Stop();
                    Log.Information($"{DateTime.Now.ToString("yyyyMMddHHmmss")} Finishing connection {connections.Count} with {sw.ElapsedMilliseconds} ms");
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
