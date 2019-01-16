using Plugin.Base;
using Rpc.Service;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods.Statistics;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.MasterMethods
{
    public class CollectStatistics : IMasterMethod
    {
        public Task Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters, IList<IRpcClient> clients)
        {

            Log.Information($"Start to collect statistics...");

            // Get parameters
            stepParameters.TryGetTypedValue(SignalRConstants.Interval, out long interval, Convert.ToInt64);
            stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
            stepParameters.TryGetTypedValue(SignalRConstants.StatisticsOutputPath, out string statisticsOutputPath, Convert.ToString);

            if (File.Exists(statisticsOutputPath))
            {
                File.Delete(statisticsOutputPath);
            }
            // Get context
            pluginParameters.TryGetTypedValue($"{SignalRConstants.LatencyStep}.{type}", out long latencyStep, Convert.ToInt64);
            pluginParameters.TryGetTypedValue($"{SignalRConstants.LatencyMax}.{type}", out long latencyMax, Convert.ToInt64);

            // Start timer
            var timer = new System.Timers.Timer();
            timer.Elapsed += async (sender, e) =>
            {
                // Start timer immedietely
                if (timer.Interval != interval) timer.Interval = interval;

                var results = await Task.WhenAll(from client in clients
                                                 select client.QueryAsync(stepParameters));

                // Merge statistics
                var merged = SignalRUtils.MergeStatistics(results, type, latencyMax, latencyStep);
                Log.Information($"Counters {Environment.NewLine}{merged.GetContents()}");

                // Display merged statistics
                Log.Information(Environment.NewLine + $"Statistic type: {type}" + Environment.NewLine + merged.GetContents());

                // Save to file
                SaveToFile(merged, statisticsOutputPath);
            };
            timer.Start();

            // Save timer to plugin
            pluginParameters[$"{SignalRConstants.Timer}.{type}"] = timer;
            return Task.CompletedTask;
        }

        private void DisplayStatistics(IDictionary<string, object>[] results, string type)
        {
            for (var i = 0; i < results.Count(); i++)
            {
                var statistics = results[i];
                Log.Information($"Type: {type} Client: {i}th statistics{Environment.NewLine}{statistics.GetContents()}");
            }
        }
        
        private void SaveToFile(IDictionary<string, long> mergedResult, string path)
        {
            var record = new JObject
            {
                { SignalRConstants.StatisticsTimestamp, Util.Timestamp2DateTimeStr(Util.Timestamp()) },
                { SignalRConstants.StatisticsCounters, JObject.FromObject(mergedResult)}
            };

            string oneLineRecord = Regex.Replace(record.ToString(), @"\s+", "");
            oneLineRecord = Regex.Replace(oneLineRecord, @"\t|\n|\r", "");
            oneLineRecord += Environment.NewLine;
            File.AppendAllText(path, oneLineRecord);
        }
    }
}
