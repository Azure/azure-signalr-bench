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
        public async Task Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters, IList<IRpcClient> clients)
        {

            Log.Information($"Start to collect statistics...");

            // Get parameters
            stepParameters.TryGetTypedValue(SignalRConstants.Interval, out long interval, Convert.ToInt64);
            stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
            stepParameters.TryGetTypedValue(SignalRConstants.StatisticsOutputPath, out string statisticsOutputPath, Convert.ToString);

            // Start timer
            var timer = new System.Timers.Timer();
            timer.Elapsed += async (sender, e) =>
            {
                // Start timer immedietely
                if (timer.Interval != interval) timer.Interval = interval;

                var results = await Task.WhenAll(from client in clients
                                                 select client.QueryAsync(stepParameters));

                // Display result for each client
                DisplayStatistics(results, type);

                // Merge statistics
                var merged = MergeStatistics(results, type);

                // Display merged statistics
                Log.Information(Environment.NewLine + $"Statistic type: {type}" + Environment.NewLine + merged.GetContents());

                // Save to file
                SaveToFile(merged, statisticsOutputPath);
            };
            timer.Start();

            // Save timer to plugin
            pluginParameters[$"{SignalRConstants.Timer}.{type}"] = timer;
        }

        private void DisplayStatistics(IDictionary<string, object>[] results, string type)
        {
            for (var i = 0; i < results.Count(); i++)
            {
                var statistics = results[i];
                Log.Information($"Type: {type} Client: {i}th statistics{Environment.NewLine}{statistics.GetContents()}");
            }
        }

        private int Sum(IDictionary<string, object>[] results, string key)
        {
            return results.ToList().Select(statistics =>
            {
                if (statistics.ContainsKey(key))
                {
                    statistics.TryGetTypedValue(key, out int item, Convert.ToInt32);
                    return item;
                }
                return 0;
            }).Sum();
        }

        private int Min(IDictionary<string, object>[] results, string key)
        {
            return results.ToList().Select(statistics =>
            {
                if (statistics.ContainsKey(key))
                {
                    statistics.TryGetTypedValue(key, out int item, Convert.ToInt32);
                    return item;
                }
                return 0;
            }).Min();
        }

        private IDictionary<string, int> MergeStatistics(IDictionary<string, object>[] results, string type)
        {
            var merged = new Dictionary<string, int>();

            // Sum of connection statistics
            merged[SignalRConstants.StatisticsConnectionConnectSuccess] = Sum(results, SignalRConstants.StatisticsConnectionConnectSuccess);
            merged[SignalRConstants.StatisticsConnectionConnectFail] = Sum(results, SignalRConstants.StatisticsConnectionConnectFail);

            // Sum of group statistics
            merged[SignalRConstants.StatisticsGroupJoinSuccess] = Sum(results, SignalRConstants.StatisticsGroupJoinSuccess);
            merged[SignalRConstants.StatisticsGroupJoinFail] = Sum(results, SignalRConstants.StatisticsGroupJoinFail);
            merged[SignalRConstants.StatisticsGroupLeaveSuccess] = Sum(results, SignalRConstants.StatisticsGroupLeaveSuccess);
            merged[SignalRConstants.StatisticsGroupLeaveFail] = Sum(results, SignalRConstants.StatisticsGroupLeaveFail);
            
            // Sum of "message:lt:latency"
            var SumMessageLatencyStatistics = (from i in Enumerable.Range(1, (int)StatisticsCollector.LatencyMax / (int)StatisticsCollector.LatencyStep)
                                               let latency = i * StatisticsCollector.LatencyStep
                                               select new { Key = SignalRUtils.MessageLessThan(latency), Sum = Sum(results, SignalRUtils.MessageLessThan(latency)) }).ToDictionary(entry => entry.Key, entry => entry.Sum);
            // Sum of "message:ge:latency"
            SumMessageLatencyStatistics[SignalRUtils.MessageGreaterOrEqaulTo(StatisticsCollector.LatencyMax)] = Sum(results, SignalRUtils.MessageGreaterOrEqaulTo(StatisticsCollector.LatencyMax));

            // Sum of total received message count
            merged[SignalRConstants.StatisticsMessageReceived] = SumMessageLatencyStatistics.Select(entry => entry.Value).Sum();

            // Sum of sent message statistics (should be calculated after "message:ge:latency")
            merged[SignalRConstants.StatisticsMessageSent] = Sum(results, SignalRConstants.StatisticsMessageSent);

            // Update epoch
            merged[SignalRConstants.StatisticsEpoch] = Min(results, SignalRConstants.StatisticsEpoch);

            merged = merged.Union(SumMessageLatencyStatistics).ToDictionary(entry => entry.Key, entry => entry.Value);

            return merged;
        }
        
        private void SaveToFile(IDictionary<string, int> mergedResult, string path)
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
