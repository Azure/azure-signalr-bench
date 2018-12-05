using Common;
using Plugin.Base;
using Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods.Statistics;
using Rpc.Service;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.MasterMethods
{
    public class ConditionalStop: IMasterMethod
    {
        public async Task Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters, IList<IRpcClient> clients)
        {
            Log.Information($"Start to conditional stop...");

            // Get parameters
            stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
            stepParameters.TryGetTypedValue(SignalRConstants.CriteriaMaxFailConnectionPercentage,
                out double criteriaMaxFailConnectionPercentage, Convert.ToDouble);
            stepParameters.TryGetTypedValue(SignalRConstants.CriteriaMaxFailConnectionAmount,
                out int criteriaMaxFailConnectionAmount, Convert.ToInt32);
            stepParameters.TryGetTypedValue(SignalRConstants.CriteriaMaxFailSendingPercentage,
                out double criteriaMaxFailSendingPercentage, Convert.ToDouble);

            // Get context
            pluginParameters.TryGetTypedValue($"{SignalRConstants.LatencyStep}.{type}",
                out long latencyStep, Convert.ToInt64);
            pluginParameters.TryGetTypedValue($"{SignalRConstants.LatencyMax}.{type}",
                out long latencyMax, Convert.ToInt64);

            var results = await Task.WhenAll(from client in clients
                                             select client.QueryAsync(stepParameters));

            // Merge statistics
            var merged = SignalRUtils.MergeStatistics(results, type, latencyMax, latencyStep);

            merged.TryGetTypedValue(SignalRConstants.StatisticsConnectionConnectSuccess,
                out int connectionSuccess, Convert.ToInt32);
            merged.TryGetTypedValue(SignalRConstants.StatisticsConnectionConnectFail,
                out int connectionFail, Convert.ToInt32);

            var connectionTotal = connectionSuccess + connectionFail;
            var connectionFailPercentage = (double)connectionFail / connectionTotal;
            var largeLatencyPercentage = GetLargeLatencyPercentage(merged, latencyMax);
            if (connectionFailPercentage > criteriaMaxFailConnectionPercentage)
            {
                var message = $"Connection fail percentage {connectionFailPercentage * 100}%" +
                              $" is greater than criteria {criteriaMaxFailConnectionPercentage * 100}%, stop benchmark";
                Log.Warning(message);
                throw new Exception(message);
            }
            if (connectionFail > criteriaMaxFailConnectionAmount)
            {
                var message = $"Connection fail amount {connectionFail}" +
                              $"is greater than {criteriaMaxFailConnectionAmount}, stop benchmark";
                Log.Warning(message);
                throw new Exception(message);
            }
             if (largeLatencyPercentage > criteriaMaxFailSendingPercentage) 
            {
                var message = $"The percentage {largeLatencyPercentage * 100}%" +
                              $"of Sending latency greater than {latencyMax}" +
                              $" ms is larger than {criteriaMaxFailSendingPercentage * 100}%, stop benchmark";
                Log.Warning(message);
                throw new Exception(message);
            }
        }

        private double GetLargeLatencyPercentage(IDictionary<string, int> data, long latencyMax)
        {
            var largeLatencyMessageCount = data[SignalRUtils.MessageGreaterOrEqaulTo(latencyMax)];
            var receivedMessageCount = data[SignalRConstants.StatisticsMessageReceived];
            return (double)largeLatencyMessageCount / receivedMessageCount;
        }
    }
}
