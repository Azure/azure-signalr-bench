using Common;
using Plugin.Base;
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
            stepParameters.TryGetTypedValue(SignalRConstants.CriteriaMaxFailConnectionPercentage, out double criteriaMaxFailConnectionPercentage, Convert.ToDouble);
            stepParameters.TryGetTypedValue(SignalRConstants.CriteriaMaxFailConnectionAmount, out int criteriaMaxFailConnectionAmount, Convert.ToInt32);

            var results = await Task.WhenAll(from client in clients
                                             select client.QueryAsync(stepParameters));

            // Merge statistics
            var merged = SignalRUtils.MergeStatistics(results, type);

            merged.TryGetTypedValue(SignalRConstants.StatisticsConnectionConnectSuccess, out int connectionSuccess, Convert.ToInt32);
            merged.TryGetTypedValue(SignalRConstants.StatisticsConnectionConnectFail, out int connectionFail, Convert.ToInt32);

            var connectionTotal = connectionSuccess + connectionFail;
            var connectionFailPercentage = (double)connectionFail / connectionTotal;
            if (connectionFailPercentage > criteriaMaxFailConnectionPercentage) throw new Exception($"Connection fail percentage {connectionFailPercentage * 100}% is greater than criteria {criteriaMaxFailConnectionPercentage * 100}%");
            if (connectionFail > criteriaMaxFailConnectionAmount) throw new Exception($"Connection fail amount {connectionFail} is greater than {criteriaMaxFailConnectionAmount}");
        }
    }
}
