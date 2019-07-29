using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.Internals
{
    public class StatisticsParser
    {
        private StatisticDictionary _prevStat;
        private StatisticDictionary _curStat;
        private StatisticDictionary _endStat;
        private long _epoch;

        public StatisticsParser()
        {
        }

        // This function is used to dump stat information after SendingStep changed during running.
        // It was not used because it will interleave with other dumping.
        public void ParseMergedDictionary(
            IDictionary<string, long> mergedResult,
            double[] percentileList,
            long latencyStep,
            long latencyMax)
        {
            _endStat = _curStat;
            _curStat = new StatisticDictionary()
            {
                Time = DateTimeOffset.UtcNow,
                Stat = new Dictionary<string, long>(mergedResult)
            };
            if (mergedResult[SignalRConstants.StatisticsEpoch] > 0)
            {
                if (mergedResult[SignalRConstants.StatisticsEpoch] != _epoch)
                {
                    if (_prevStat != null)
                    {
                        PrintStat(_prevStat, _endStat, percentileList, latencyStep, latencyMax);
                    }
                    _prevStat = _curStat;
                }
                _epoch = mergedResult[SignalRConstants.StatisticsEpoch];
            }
        }

        public static void PrintStat(
            StatisticDictionary begin,
            StatisticDictionary end,
            double[] percentileList,
            long latencyStep,
            long latencyMax)
        {
            var elapse = end.Time - begin.Time;
            var sentMsgSize = end.Stat[SignalRConstants.StatisticsMessageSentSize] -
                begin.Stat[SignalRConstants.StatisticsMessageSentSize];
            var recvMsgSize = end.Stat[SignalRConstants.StatisticsMessageReceivedSize] -
                begin.Stat[SignalRConstants.StatisticsMessageReceivedSize];
            var sent = end.Stat[SignalRConstants.StatisticsMessageSent] -
                begin.Stat[SignalRConstants.StatisticsMessageSent];
            var recv = end.Stat[SignalRConstants.StatisticsMessageReceived] -
                begin.Stat[SignalRConstants.StatisticsMessageReceived];
            var connection = end.Stat[SignalRConstants.StatisticsConnectionConnectSuccess];
            var sendingStep = end.Stat[SignalRConstants.StatisticsSendingStep];
            var sendTputs = sentMsgSize / elapse.TotalSeconds;
            var recvTputs = recvMsgSize / elapse.TotalSeconds;
            var sendRate = sent / elapse.TotalSeconds;
            var recvRate = recv / elapse.TotalSeconds;
            Log.Information($"-----------");
            Log.Information($" Connection/sendingStep: {connection}/{sendingStep} in {elapse.TotalSeconds}s");
            Log.Information($" Messages: requests: {FormatBytesDisplay(sentMsgSize)}, responses: {FormatBytesDisplay(recvMsgSize)}");
            Log.Information($"   Requests/sec: {FormatDoubleValue(sendRate)}");
            Log.Information($"   Responses/sec: {FormatDoubleValue(recvRate)}");
            Log.Information($"   Write throughput: {FormatBytesDisplay(sendTputs)}");
            Log.Information($"   Read throughput: {FormatBytesDisplay(recvTputs)}");
            Log.Information($" Latency:");
            foreach (var p in percentileList)
            {
                var index = FindLatencyLowerBound(end.Stat, p, latencyStep, latencyMax);
                if (index == latencyStep + latencyMax)
                {
                    Log.Information($"  {p * 100}%: >= 1s");
                }
                else if (index == latencyStep)
                {
                    Log.Information($"  {p * 100}%: < {index} ms");
                }
                else
                {
                    Log.Information($"  {p * 100}%: < {index + latencyStep} ms");
                }
            }
        }

        private static long FindLatencyLowerBound(
            IDictionary<string, long> stat,
            double percentile,
            long latencyStep,
            long latencyMax)
        {
            long total = 0;
            double sum = 0.0;
            for (var i = latencyStep; i <= latencyMax; i += latencyStep)
            {
                total += stat[$"{SignalRConstants.StatisticsLatencyLessThan}{i}"];
            }
            total += stat[$"{SignalRConstants.StatisticsLatencyGreatEqThan}{latencyMax}"];
            for (var i = latencyStep; i <= latencyMax; i += latencyStep)
            {
                sum += stat[$"{SignalRConstants.StatisticsLatencyLessThan}{i}"];
                if (sum >= total * percentile)
                    return i;
            }
            return latencyMax + latencyStep;
        }
        
        // This function dump stat information after successful running.
        // If it was interrupted, this dump may not get a chance to run.
        public static void Parse(
            string fileName,
            double[] percentileList,
            long latencyStep,
            long latencyMax,
            Action<string> output,
            bool outputJsonFormat = false)
        {
            Statistics firstStat = null, prevStat = null, endStat = null, curStat = null;
            long prevEpoch = 0, totalConnection = 0;
            long reconnectInThisEpoch = 0, connectCostInThisEpoch = 0, reconnectCostInThisEpoch = 0;
            var benchItemList = new List<BenchResultItem>();
            Action<string> outputEveryEpoch = output;
            if (outputJsonFormat)
            {
                outputEveryEpoch = null;
            }
            foreach (var statistic in GetStatistics(fileName))
            {
                if (firstStat == null)
                {
                    firstStat = statistic;
                }
                endStat = curStat;
                curStat = statistic;
                if (curStat.Counters.Epoch > 0)
                {
                    if (curStat.Counters.Epoch == prevEpoch)
                    {
                        reconnectInThisEpoch = curStat.Counters.ConnectionReconnect;
                        if (curStat.Counters.ConnectCost99Percent > connectCostInThisEpoch)
                        {
                            connectCostInThisEpoch = curStat.Counters.ConnectCost99Percent;
                        }
                        if (curStat.Counters.ReconnectCost99Percent > reconnectCostInThisEpoch)
                        {
                            reconnectCostInThisEpoch = curStat.Counters.ReconnectCost99Percent;
                        }
                        continue;
                    }
                    if (prevStat != null)
                    {
                        var item = PrintEpoch(prevStat,
                                              endStat,
                                              percentileList,
                                              latencyStep,
                                              latencyMax,
                                              reconnectInThisEpoch,
                                              connectCostInThisEpoch,
                                              reconnectCostInThisEpoch,
                                              outputEveryEpoch);
                        benchItemList.Add(item);
                    }
                    else
                    {
                        if (firstStat != null)
                        {
                            totalConnection = PrintConnectionEstablishedStat(firstStat, endStat, outputEveryEpoch);
                        }
                    }
                    prevEpoch = curStat.Counters.Epoch;
                    prevStat = curStat;
                    reconnectInThisEpoch = 0;
                    connectCostInThisEpoch = 0;
                    reconnectCostInThisEpoch = 0;
                }
            }
            if (endStat != null && prevStat != null && endStat != prevStat)
            {
                var item = PrintEpoch(prevStat, endStat, percentileList,
                                      latencyStep, latencyMax,
                                      reconnectInThisEpoch,
                                      connectCostInThisEpoch,
                                      reconnectCostInThisEpoch,
                                      outputEveryEpoch);
                benchItemList.Add(item);
            }
            if (outputJsonFormat)
            {
                var benchResult = new BenchResult()
                {
                    Connections = totalConnection,
                    Items = benchItemList.ToArray()
                };
                var result = JsonConvert.SerializeObject(benchResult);
                output(result);
            }
        }

        private static long PrintConnectionEstablishedStat(
            Statistics start,
            Statistics end,
            Action<string> outputCallback)
        {
            var elapse = DateTimeOffset.Parse(end.Time) - DateTimeOffset.Parse(start.Time);
            var succ = end.Counters.ConnectionSuccess;
            if (outputCallback != null)
            {
                outputCallback($"-----------");
                outputCallback($"  {succ} connections established in {elapse.TotalSeconds}s");
            }
            return succ;
        }

        private static BenchResultItem PrintEpoch(
            Statistics start,
            Statistics end,
            double[] percentileList,
            long latencyStep,
            long latencyMax,
            long reconnectInThisEpoch,
            long connectCost99PercentInThisEpoch,
            long reconnectCost99PercentInThisEpoch,
            Action<string> outputCallback)
        {
            var elapse = DateTimeOffset.Parse(end.Time) - DateTimeOffset.Parse(start.Time);
            var sentMsgSize = end.Counters.MessageSentSize;
            var recvMsgSize = end.Counters.MessageRecvSize;
            var sent = end.Counters.MessageSent;
            var recv = end.Counters.MessageReceived;
            var connections = end.Counters.ConnectionSuccess;
            var sendingStep = end.Counters.SendingStep;
            var sendTputs = sentMsgSize / elapse.TotalSeconds;
            var recvTputs = recvMsgSize / elapse.TotalSeconds;
            var sendRate = sent / elapse.TotalSeconds;
            var recvRate = recv / elapse.TotalSeconds;
            var percentList = new List<double>();
            var latDist = new List<long>();
            foreach (var p in percentileList)
            {
                var index = FindLatencyLowerBound(end.Counters, p, latencyStep, latencyMax);
                if (index == latencyStep + latencyMax)
                {
                    var lt1s = FindLatencyLessThan1sPercent(end.Counters, latencyStep, latencyMax);
                    percentList.Add(lt1s);
                    latDist.Add(1000);
                    break;
                }
                else
                {
                    percentList.Add(p);
                    latDist.Add(index);
                }
            }
            if (outputCallback != null)
            {
                outputCallback($"-----------");
                outputCallback($" Connections/sendingStep: {connections}/{sendingStep} in {elapse.TotalSeconds}s");
                outputCallback($" Messages: requests: {FormatBytesDisplay(sentMsgSize)}, responses: {FormatBytesDisplay(recvMsgSize)}");
                outputCallback($"   Requests/sec: {FormatDoubleValue(sendRate)}");
                outputCallback($"   Responses/sec: {FormatDoubleValue(recvRate)}");
                outputCallback($"   Write throughput: {FormatBytesDisplay(sendTputs)}");
                outputCallback($"   Read throughput: {FormatBytesDisplay(recvTputs)}");
                outputCallback($" Latency:");
                percentList.ToList().ForEach(x =>
                   outputCallback($"  {FormatDoubleValue(x * 100)}%: < {latDist[percentList.IndexOf(x)]} ms"));
                if (reconnectInThisEpoch > 0)
                {
                    outputCallback($" Reconnect: {reconnectInThisEpoch}");
                }
                if (connectCost99PercentInThisEpoch > 0)
                {
                    outputCallback($" 99% connect cost (ms): {connectCost99PercentInThisEpoch}");
                }
                if (reconnectCost99PercentInThisEpoch > 0)
                {
                    outputCallback($" 99% reconnect cost (ms): {reconnectCost99PercentInThisEpoch}");
                }
            }
            var msg = new MessageBenchResult()
            {
                TotalRecv = recv,
                TotalRecvMsgSize = recvMsgSize,
                TotalSend = sent,
                TotalSendMsgSize = sentMsgSize
            };
            var lat = new LatencyBenchResult()
            {
                PercentileList = percentList.ToArray(),
                LatencyList = latDist.ToArray()
            };
            var benchItem = new BenchResultItem()
            {
                SendingStep = sendingStep,
                Duration = elapse.TotalMilliseconds,
                Message = msg,
                Latency = lat,
                Reconnect = reconnectInThisEpoch,
                ConnectionCost99Percent = connectCost99PercentInThisEpoch,
                ReconnectionCost99Percent = reconnectCost99PercentInThisEpoch
            };
            return benchItem;
        }

        private static string FormatDoubleValue(double value)
        {
            return String.Format("{0:0.00}", value);
        }

        private static string FormatBytesDisplay(double value)
        {
            var ret = $"{value}B";
            if (value > 1000000000)
            {
                value /= 1000000000.0;
                var str = FormatDoubleValue(value);
                ret = $"{str}GB";
            }
            else if (value > 1000000)
            {
                value /= 1000000.0;
                var str = FormatDoubleValue(value);
                ret = $"{str}MB";
            }
            else if (value > 1000)
            {
                value /= 1000.0;
                var str = FormatDoubleValue(value);
                ret = $"{str}KB";
            }
            else
            {
                var str = FormatDoubleValue(value);
                ret = $"{str}B";
            }
            return ret;
        }

        protected static long FindLatencyLowerBound(
            Statistic counter,
            double p,
            long latencyStep,
            long latencyMax)
        {
            long total = counter.MessageReceived, sum = 0;
            for (var i = latencyStep; i <= latencyMax; i += latencyStep)
            {
                sum += Convert.ToInt64(counter.GetType().GetProperty($"MessageLatencyLt{i}").GetValue(counter));
                if (sum >= (total * p))
                {
                    return i;
                }
            }
            return latencyStep + latencyMax;
        }

        protected static double FindLatencyLessThan1sPercent(
            Statistic counter,
            long latencyStep,
            long latencyMax)
        {
            long total = counter.MessageReceived, sum = 0;
            for (var i = latencyStep; i <= latencyMax; i += latencyStep)
            {
                sum += Convert.ToInt64(counter.GetType().GetProperty($"MessageLatencyLt{i}").GetValue(counter));
            }
            var s = (double)sum;
            return s / total;
        }

        private static IEnumerable<Statistics> GetStatistics(string fileName)
        {
            string line;
            var fileHandle = new StreamReader(fileName);
            try
            {
                while ((line = fileHandle.ReadLine()) != null)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        var statistics = JsonConvert.DeserializeObject<Statistics>(line);
                        yield return statistics;
                    }
                }
            }
            finally
            {
                fileHandle.Close();
            }
        }
    }
}
