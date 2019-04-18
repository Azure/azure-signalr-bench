using Common;
using Newtonsoft.Json.Linq;
using Rpc.Service;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.MasterMethods
{
    public class CollectStatisticBase
    {
        private long _latencyStep;
        private long _latencyMax;
        private long _interval;
        private string _type;
        private string _statisticsOutputPath;
        private double[] _percentileList;

        protected void ExtractParams(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters)
        {
            // Get parameters
            stepParameters.TryGetTypedValue(SignalRConstants.Interval, out long interval, Convert.ToInt64);
            stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
            stepParameters.TryGetTypedValue(SignalRConstants.StatisticsOutputPath, out string statisticsOutputPath, Convert.ToString);
            _statisticsOutputPath = statisticsOutputPath;
            _interval = interval;
            _type = type;

            // Get context
            pluginParameters.TryGetTypedValue($"{SignalRConstants.LatencyStep}.{_type}", out long latencyStep, Convert.ToInt64);
            pluginParameters.TryGetTypedValue($"{SignalRConstants.LatencyMax}.{_type}", out long latencyMax, Convert.ToInt64);
            _latencyMax = latencyMax;
            _latencyStep = latencyStep;

            if (stepParameters.TryGetValue(SignalRConstants.PercentileList, out _))
            {
                stepParameters.TryGetTypedValue(SignalRConstants.PercentileList, out string percentileListStr, Convert.ToString);
                _percentileList = percentileListStr.Split(",").Select(ind => Convert.ToDouble(ind)).ToArray();
            }
        }

        protected void ClearOldStatistics()
        {
            if (File.Exists(_statisticsOutputPath))
            {
                File.Delete(_statisticsOutputPath);
            }
        }

        protected void CollectStatistics(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters,
            IList<IRpcClient> clients,
            Func<IDictionary<string, object>, IDictionary<string, object>, IList<IRpcClient>, Task> callback)
        {
            ExtractParams(stepParameters, pluginParameters);
            ClearOldStatistics();
            // Start timer
            var timer = new System.Timers.Timer(_interval);
            timer.Elapsed += async (sender, e) =>
                await callback(stepParameters, pluginParameters, clients);
            timer.Start();

            // Save timer to plugin
            pluginParameters[$"{SignalRConstants.Timer}.{_type}"] = timer;
        }

        protected async Task LatencyEventerCallback(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters, IList<IRpcClient> clients)
        {
            var results = await Task.WhenAll(from client in clients
                                             select client.QueryAsync(stepParameters));

            // Merge statistics
            var merged = SignalRUtils.MergeStatistics(results, _latencyMax, _latencyStep);
            // Display merged statistics
            Log.Information(Environment.NewLine + $"Statistic type: {_type}" + Environment.NewLine + merged.GetContents());

            // Save to file
            SaveToFile(merged, _statisticsOutputPath);
        }

        protected async Task ConnectionStatEventerCallback(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters, IList<IRpcClient> clients)
        {
            var results = await Task.WhenAll(from client in clients
                                             select client.QueryAsync(stepParameters));
            // Merge statistics
            var merged = SignalRUtils.MergeStatistics(results, _latencyMax, _latencyStep);
            var connectionStatMerged = SignalRUtils.MergeConnectionStatistics(results, _percentileList.ToArray());
            merged = merged.Union(connectionStatMerged).ToDictionary(entry => entry.Key, entry => entry.Value);
            // Display merged statistics
            Log.Information(Environment.NewLine + $"Statistic type: {_type}" + Environment.NewLine + merged.GetContents());

            // Save to file
            SaveToFile(merged, _statisticsOutputPath);
        }

        protected void SaveToFile(IDictionary<string, long> mergedResult, string path)
        {
            var record = new JObject
            {
                { SignalRConstants.StatisticsTimestamp, Util.Timestamp2DateTimeStr(Util.Timestamp()) },
                { SignalRConstants.StatisticsCounters, JObject.FromObject(mergedResult)}
            };

            string oneLineRecord = Regex.Replace(record.ToString(), @"\s+", "");
            oneLineRecord = Regex.Replace(oneLineRecord, @"\t|\n|\r", "");
            oneLineRecord += Environment.NewLine;
            lock (this)
            {
                using (StreamWriter sw = new StreamWriter(path, true))
                {
                    sw.Write(oneLineRecord);
                }
            }
        }
    }
}
