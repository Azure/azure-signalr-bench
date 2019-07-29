using Common;
using Plugin.Microsoft.Azure.SignalR.Benchmark.AgentMethods.Statistics;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.AgentMethods
{
    public class InitStatisticsCollectorBase
    {
        private string _type;
        private long _latencyStep;
        private long _latencyMax;

        protected void ExtracParam(
            IDictionary<string, object> stepParameters)
        {
            // Get parameters
            stepParameters.TryGetTypedValue(SignalRConstants.Type,
                out string type, Convert.ToString);
            stepParameters.TryGetTypedValue($"{SignalRConstants.LatencyStep}",
                out long latencyStep, Convert.ToInt64);
            stepParameters.TryGetTypedValue($"{SignalRConstants.LatencyMax}",
                out long latencyMax, Convert.ToInt64);
            _type = type;
            _latencyMax = latencyMax;
            _latencyStep = latencyStep;
        }

        public Task<IDictionary<string, object>> Run(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters,
            Action<IDictionary<string, object>, IDictionary<string, object>> callback)
        {
            try
            {
                ExtracParam(stepParameters);
                callback.Invoke(stepParameters, pluginParameters);
                return Task.FromResult<IDictionary<string, object>>(null);
            }
            catch (Exception ex)
            {
                var message = $"Fail to init statistic collector: {ex}";
                Log.Error(message);
                throw;
            }
        }

        protected void RegisterLatencyStatistics(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters)
        {
            // Init statistic collector
            pluginParameters[$"{SignalRConstants.StatisticsStore}.{_type}"] =
                new StatisticsCollector(_latencyStep, _latencyMax);
        }

        protected void RegisterConnectionStatistics(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters)
        {
            pluginParameters.TryGetTypedValue(
                $"{SignalRConstants.ConnectionStore}.{_type}",
                out IList<IHubConnectionAdapter> connections,
                (obj) => (IList<IHubConnectionAdapter>)obj);
            // Init statistic collector
            pluginParameters[$"{SignalRConstants.StatisticsStore}.{_type}"] =
                new ConnectionStatisticCollector(connections, _latencyStep, _latencyMax);
        }
    }
}
