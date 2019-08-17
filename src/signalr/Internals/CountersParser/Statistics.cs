using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.Internals
{
    public class StatisticDictionary
    {
        public DateTimeOffset Time { get; set; }
        public IDictionary<string, long> Stat { get; set; }
    }

    public class Statistics
    {
        public string Time { get; set; }
        public Statistic Counters { get; set; }
    }

    public class BenchResult
    {
        public long Connections { get; set; }

        public BenchResultItem[] Items { get; set; }
    }

    public class BenchResultItem
    {
        public long SendingStep { get; set; }
        public double Duration { get; set; } // milli-seconds
        public long Reconnect { get; set; }
        public long ConnectionCost99Percent { get; set; }
        public long ReconnectionCost99Percent { get; set; }
        public MessageBenchResult Message { get; set; }
        public LatencyBenchResult Latency { get; set; }
    }

    public class MessageBenchResult
    {
        public long TotalSend { get; set; }
        public long TotalRecv { get; set; }
        public long TotalSendMsgSize { get; set; }
        public long TotalRecvMsgSize { get; set; }
    }

    public class LatencyBenchResult
    {
        public double[] PercentileList { get; set; }
        public long[] LatencyList { get; set; }
    }

    public class Statistic
    {
        [JsonProperty(PropertyName = SignalRConstants.StatisticsConnectionConnectSuccess)]
        public long ConnectionSuccess { get; set; }

        [JsonProperty(PropertyName = SignalRConstants.StatisticsConnectionConnectFail)]
        public long ConnectionFail { get; set; }

        [JsonProperty(PropertyName = SignalRConstants.StatisticsConnectionReconnect)]
        public long ConnectionReconnect { get; set; }

        [JsonProperty(PropertyName = SignalRConstants.StatisticsMessageReceived)]
        public long MessageReceived { get; set; }

        [JsonProperty(PropertyName = SignalRConstants.StatisticsMessageSent)]
        public long MessageSent { get; set; }

        [JsonProperty(PropertyName = SignalRConstants.StatisticsMessageSentSize)]
        public long MessageSentSize { get; set; }

        [JsonProperty(PropertyName = SignalRConstants.StatisticsMessageReceivedSize)]
        public long MessageRecvSize { get; set; }

        [JsonProperty(PropertyName = SignalRConstants.StatisticsEpoch)]
        public long Epoch { get; set; }

        [JsonProperty(PropertyName = SignalRConstants.StatisticsSendingStep)]
        public long SendingStep { get; set; }

        [JsonProperty(PropertyName = "message:lt:100")]
        public long MessageLatencyLt100 { get; set; }

        [JsonProperty(PropertyName = "message:lt:200")]
        public long MessageLatencyLt200 { get; set; }

        [JsonProperty(PropertyName = "message:lt:300")]
        public long MessageLatencyLt300 { get; set; }

        [JsonProperty(PropertyName = "message:lt:400")]
        public long MessageLatencyLt400 { get; set; }

        [JsonProperty(PropertyName = "message:lt:500")]
        public long MessageLatencyLt500 { get; set; }

        [JsonProperty(PropertyName = "message:lt:600")]
        public long MessageLatencyLt600 { get; set; }

        [JsonProperty(PropertyName = "message:lt:700")]
        public long MessageLatencyLt700 { get; set; }

        [JsonProperty(PropertyName = "message:lt:800")]
        public long MessageLatencyLt800 { get; set; }

        [JsonProperty(PropertyName = "message:lt:900")]
        public long MessageLatencyLt900 { get; set; }

        [JsonProperty(PropertyName = "message:lt:1000")]
        public long MessageLatencyLt1000 { get; set; }

        [JsonProperty(PropertyName = "message:ge:1000")]
        public long MessageLatencyGe1000 { get; set; }

        [JsonProperty(PropertyName = "connection:connect:cost:0.99")]
        public long ConnectCost99Percent { get; set; }

        [JsonProperty(PropertyName = "connection:reconnect:cost:0.99")]
        public long ReconnectCost99Percent { get; set; }
    }
}
