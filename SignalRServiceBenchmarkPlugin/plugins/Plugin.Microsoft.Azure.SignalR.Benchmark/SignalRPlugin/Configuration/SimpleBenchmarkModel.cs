using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    public class SimpleBenchmarkModel
    {
        // default settings
        public const int DEFAULT_BASE_SENDING_STEP = 500;
        public const int DEFAULT_STEP = 500;
        public const int DEFAULT_SENDING_STEPS = 2;
        public const int DEFAULT_CONNECTIONS = 1000;
        public const int DEFAULT_ARRIVINGRATE = 50;
        public const int DEFAULT_MESSAGESIZE = 2048;
        public const int DEFAULT_SINGLE_STEP_DUR = 240000;
        public const int DEFAULT_SEND_INTERVAL = 1000;
        public const int DEFAULT_ARRIVING_BATCH_WAIT = 1000;
        public const double DEFAULT_CONNECT_FAIL_PERCENTAGE = 0.01;
        public const double DEFAULT_MESSAGE_LATENCY_PERCENTAGE = 0.01;
        public const string DEFAULT_KIND = "perf";
        public const string LONGRUN_KIND = "longrun";
        public const string DEFAULT_MODE = "simple";
        public const string DEFAULT_TRANSPORT = "Websockets";
        public const string SSE_TRANSPORT = "ServerSentEvents";
        public const string LONGPOLLING_TRANSPORT = "LongPolling";
        public const string DEFAULT_PROTOCOL = "json";
        public const string MSGPACK_PROTOCOL = "messagepack";
        public const string DEFAULT_CONNECTION_TYPE = "Core";
        public const string ASPNET_CONNECTION_TYPE = "AspNet";
        public const string DIRECT_CONNECTION_PREFIX = "rest";
        public const string DEFAULT_ARRIVING_BATCH_MODE = "HighPress";
        public const string DEFAULT_SCENARIO = "echo";

        public class BenchConfigData
        {
            public string Mode { get; set; } = DEFAULT_MODE;
            public string Kind { get; set; } = DEFAULT_KIND;
            public ConfigData Config { get; set; }
            public ScenarioData Scenario { get; set; }
        }

        public class ConfigData
        {
            public string ConnectionString { get; set; }
            public string WebAppTarget { get; set; }
            public string Transport { get; set; } = DEFAULT_TRANSPORT;
            public string Protocol { get; set; } = DEFAULT_PROTOCOL;
            public string ConnectionType { get; set; } = DEFAULT_CONNECTION_TYPE;
            public int ArrivingRate { get; set; } = DEFAULT_ARRIVINGRATE;

            public string ArrivingBatchMode { get; set; } = DEFAULT_ARRIVING_BATCH_MODE;

            public int ArrivingBatchWait { get; set; } = DEFAULT_ARRIVING_BATCH_WAIT;

            public int SingleStepDuration { get; set; } = DEFAULT_SINGLE_STEP_DUR;

            public int Connections { get; set; } = DEFAULT_CONNECTIONS;

            public int BaseSending { get; set; } = DEFAULT_BASE_SENDING_STEP;

            public int Step { get; set; } = DEFAULT_STEP;

            public int SendingSteps { get; set; } = DEFAULT_SENDING_STEPS;

            public double ConnectionFailPercentage { get; set; } = DEFAULT_CONNECT_FAIL_PERCENTAGE;

            public double LatencyPercentage { get; set; } = DEFAULT_MESSAGE_LATENCY_PERCENTAGE;

        }

        public class ScenarioData
        {
            public string Name { get; set; } = DEFAULT_SCENARIO;
            public ScenarioParameters Parameters { get; set; } = new ScenarioParameters();
        }

        public class ScenarioParameters
        {
            public int MessageSize { get; set; } = DEFAULT_MESSAGESIZE;
            public int SendingInterval { get; set; } = DEFAULT_SEND_INTERVAL;
            public int GroupCount { get; set; }
        }

        public SimpleBenchmarkModel()
        {
        }

        public bool isCore(BenchConfigData configData)
        {
            return configData.Config.ConnectionType == DEFAULT_CONNECTION_TYPE;
        }

        public bool isSimple(BenchConfigData configData)
        {
            return configData.Mode == DEFAULT_MODE;
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

        public void Dump(BenchConfigData config)
        {
            Console.WriteLine($"mode: {config.Mode}");
            Console.WriteLine($"kind: {config.Kind}");
            Console.WriteLine("config: ");
            Console.WriteLine(" arrivingBatchMode: " + config.Config.ArrivingBatchMode);
            Console.WriteLine(" arrivingRate: " + config.Config.ArrivingRate);
            Console.WriteLine(" baseSending: " + config.Config.BaseSending);
            Console.WriteLine(" connections: " + config.Config.Connections);
            Console.WriteLine(" connectionString: " + config.Config.ConnectionString);
            Console.WriteLine(" connectionType: " + config.Config.ConnectionType);
            Console.WriteLine(" protocol: " + config.Config.Protocol);
            Console.WriteLine(" singleStepDuration: " + config.Config.SingleStepDuration);
            Console.WriteLine(" transport: " + config.Config.Transport);
            Console.WriteLine(" webAppTarget: " + config.Config.WebAppTarget);

            Console.WriteLine("scenario: ");
            Console.WriteLine(" name: " + config.Scenario.Name);
            var para = config.Scenario.Parameters;
            Console.WriteLine(" parameters:");
            Console.WriteLine("   messageSize: " + para.MessageSize);
            Console.WriteLine("   sendingInterval: " + para.SendingInterval);
        }
    }
}
