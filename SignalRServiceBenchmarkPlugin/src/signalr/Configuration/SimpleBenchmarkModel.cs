using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    public class SimpleBenchmarkModel
    {
        // default settings
        public const uint DEFAULT_BASE_SENDING_STEP = 500;
        public const uint DEFAULT_STEP = 500;
        public const uint DEFAULT_SENDING_STEPS = 0;
        public const uint DEFAULT_CONNECTIONS = 1000;
        public const uint DEFAULT_ARRIVINGRATE = 50;
        public const uint DEFAULT_MESSAGESIZE = 2048;
        public const uint DEFAULT_SINGLE_STEP_DUR = 240000;
        public const uint DEFAULT_SEND_INTERVAL = 1000;
        public const uint DEFAULT_ARRIVING_BATCH_WAIT = 1000;
        public const double DEFAULT_CONNECT_FAIL_PERCENTAGE = 0.01;
        public const double DEFAULT_MESSAGE_LATENCY_PERCENTAGE = 0.01;
        public const string DEFAULT_KIND = "perf";
        public const string STRICTPERF_KIND = "perf2";
        public const string PARSERESULT_KIND = "resultparser";
        public const string DEFAULT_MODE = "simple";
        public const string ADVANCED_MODE = "advanced";
        public const string DEFAULT_TRANSPORT = "Websockets";
        public const string SSE_TRANSPORT = "ServerSentEvents";
        public const string LONGPOLLING_TRANSPORT = "LongPolling";
        public const string DEFAULT_PROTOCOL = "json";
        public const string MSGPACK_PROTOCOL = "messagepack";
        public const string DEFAULT_CONNECTION_TYPE = "Core";
        public const string ASPNET_CONNECTION_TYPE = "AspNet";
        public const string DIRECT_CONNECTION_PREFIX = "rest";
        public const string DEFAULT_ARRIVING_BATCH_MODE = "HighPress";
        public const string LOW_ARRIVING_BATCH_MODE = "LowPress";
        public const string DEFAULT_SCENARIO = "echo";
        public const string DEFAULT_OUTPUT_PATH = "counters.txt";
        public const string DEFAULT_WEBAPP_HUB_URL = "http://localhost:5050/signalrbench";

        public class BenchConfigData
        {
            public string Mode { get; set; } = DEFAULT_MODE;
            public string Kind { get; set; } = DEFAULT_KIND;
            public ConfigData Config { get; set; } = new ConfigData();
            public ScenarioData Scenario { get; set; } = new ScenarioData();
        }

        public class ConfigData
        {
            public string ConnectionString { get; set; }
            public string WebAppTarget { get; set; }
            public string Transport { get; set; } = DEFAULT_TRANSPORT;
            public string Protocol { get; set; } = DEFAULT_PROTOCOL;
            public string ConnectionType { get; set; } = DEFAULT_CONNECTION_TYPE;
            public uint ArrivingRate { get; set; } = DEFAULT_ARRIVINGRATE;

            public string ArrivingBatchMode { get; set; } = DEFAULT_ARRIVING_BATCH_MODE;

            public uint ArrivingBatchWait { get; set; } = DEFAULT_ARRIVING_BATCH_WAIT;

            public uint SingleStepDuration { get; set; } = DEFAULT_SINGLE_STEP_DUR;

            public uint Connections { get; set; } = DEFAULT_CONNECTIONS;

            public uint BaseSending { get; set; } = DEFAULT_BASE_SENDING_STEP;

            public uint Step { get; set; } = DEFAULT_STEP;

            public uint SendingSteps { get; set; } = DEFAULT_SENDING_STEPS;

            public double ConnectionFailPercentage { get; set; } = DEFAULT_CONNECT_FAIL_PERCENTAGE;

            public double LatencyPercentage { get; set; } = DEFAULT_MESSAGE_LATENCY_PERCENTAGE;

            public string ResultFilePath { get; set; } = DEFAULT_OUTPUT_PATH;

            public bool Debug { get; set; } = false;
        }

        public class ScenarioData
        {
            public string Name { get; set; } = DEFAULT_SCENARIO;
            public ScenarioParameters Parameters { get; set; } = new ScenarioParameters();
        }

        public class ScenarioParameters
        {
            public uint MessageSize { get; set; } = DEFAULT_MESSAGESIZE;
            public uint SendingInterval { get; set; } = DEFAULT_SEND_INTERVAL;
            public uint GroupCount { get; set; }
        }

        public SimpleBenchmarkModel()
        {
        }

        public BenchConfigData Deserialize(string content)
        {
            var input = new StringReader(content);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();
            var config = deserializer.Deserialize<BenchConfigData>(input);
            return config;
        }
    }
}
