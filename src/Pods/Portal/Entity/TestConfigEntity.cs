using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Azure.SignalRBench.Common;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using Portal;

namespace Azure.SignalRBench.Coordinator.Entities
{
    public class TestConfigEntity : TableEntity
    {
        public static Regex TagsRegex =
            new Regex("^(([a-zA-Z0-9]{1,}=[a-zA-Z0-9]{1,};){0,}([a-zA-Z0-9]{1,}=[a-zA-Z0-9]{1,}){0,1})$");

        public string? User { get; set; }

        public string Service { get; set; } = "SignalR";
        public string Mode { get; set; } = "Default";

        public string Framework { get; set; } = "Netcore";

        public int ClientCons { get; set; } = 1000;

        public int ConnectEstablishRoundNum { get; set; } = 1;

        public string? ConnectionString { get; set; }

        public int SignalRUnitSize { get; set; }

        public int ServerNum { get; set; } = -1;

        public int ClientNum { get; set; } = -1;

        public int InstanceIndex { get; set; } = 0;

        public int Start { get; set; } = 1;

        public int RoundNum { get; set; } = 5;

        public int End { get; set; } = -1;

        public int GroupSize { get; set; } = 100;

        public string Scenario { get; set; } = ClientBehavior.Echo.ToString();

        //seconds
        public int RoundDurations { get; set; } = 60;

        public int Interval { get; set; } = 1000;

        public int MessageSize { get; set; } = 2048;

        public string Protocol { get; set; } = Common.Protocol.WebSocketsWithJson.ToString();

        public int Rate { get; set; } = 200;

        public string Cron { get; set; } = "0";

        public string LastCronTime { get; set; } = "0";

        public string Dir { get; set; } = "Default";

        public string Env { get; set; } = PerfConstants.Cloud.AzureGlobal;
        public string Tags { get; set; } = "";

        public void Init()
        {
            Start = Start > ClientCons ? ClientCons : Start;
            End = End > ClientCons ? ClientCons : End;
            End = End < Start ? Start : End;
            if (ConnectEstablishRoundNum < 1)
                ConnectEstablishRoundNum = 1;
            if (ConnectEstablishRoundNum > RoundNum)
                ConnectEstablishRoundNum = RoundNum;
            if (ClientNum <= 0)
                ClientNum = (int) Math.Ceiling((double) ClientCons / PerfConstants.Number.ConnectionsPerClient);

            if (ServerNum <= 0) ServerNum = (int) Math.Ceiling((double) ClientNum / 2);

            if (RoundNum <= 0) RoundNum = 5;
            if (!TagsRegex.IsMatch(Tags)) throw new Exception("Invalid tags pattern");
        }

        public TestJob ToTestJob(ClusterState clusterState)
        {
            //creating round settings
            var roundsettings = new List<RoundSetting>();
            var current = Start;
            var step = RoundNum > 1 ? (int) Math.Ceiling((double) (End - Start) / (RoundNum - 1)) : 0;
            if (!Enum.TryParse(Scenario, out ClientBehavior behavior))
                throw new Exception($"Unknown Scenario {Scenario}");
            if (!Enum.TryParse(Mode, out SignalRServiceMode serviceMode))
                throw new Exception($"Unknown Service mode {serviceMode}");
            var testCategory = TestCategory.AspnetCoreSignalR;
            switch (Service)
            {
                case "RawWebsocket":
                    testCategory = TestCategory.RawWebsocket;
                    ServerNum = 0;
                    break;
                case "SignalR" when serviceMode == SignalRServiceMode.Serverless:
                    testCategory = TestCategory.AspnetCoreSignalRServerless;
                    break;
                case "SignalR":
                    testCategory = Framework == "Netcore" ? TestCategory.AspnetCoreSignalR : TestCategory.AspnetSignalR;
                    break;
            }

            for (var i = 0; i < RoundNum; i++)
            {
                var count = current > End ? End : current;
                roundsettings.Add(new RoundSetting
                {
                    DurationInSeconds = RoundDurations,
                    ClientSettings = new[]
                    {
                        new ClientSetting
                        {
                            Behavior = behavior,
                            IntervalInMilliseconds = Interval,
                            Count = count,
                            MessageSize = MessageSize,
                            GroupFamily = "default"
                        }
                    }
                });
                current += step;
            }

            var testJob = new TestJob
            {
                TestId = PartitionKey + '-' + InstanceIndex,
                TestMethod = testCategory,
                ServiceSetting = new[]
                {
                    new ServiceSetting
                    {
                        AsrsConnectionString = ConnectionString?.Trim(),
                        Location = Env.ToLower().Contains("ppe") ? clusterState.PPELocation : clusterState.Location,
                        Tier = "standard",
                        Size = SignalRUnitSize,
                        Env = Env,
                        Tags = Tags
                    }
                },
                ScenarioSetting = new ScenarioSetting
                {
                    TotalConnectionCount = ClientCons,
                    TotalConnectionRound = ConnectEstablishRoundNum,
                    Rounds = roundsettings.ToArray(),
                    IsAnonymous = true,
                    Protocol = Enum.TryParse(Protocol, out Protocol protocol)
                        ? protocol
                        : throw new Exception($"Unknown Protocol {Protocol}"),
                    Rate = Rate,
                    GroupDefinitions = behavior == ClientBehavior.GroupBroadcast
                        ? new[]
                        {
                            new GroupDefinition
                            {
                                GroupFamily = "default",
                                GroupCount = 0,
                                GroupSize = GroupSize
                            }
                        }
                        : Array.Empty<GroupDefinition>()
                },
                PodSetting = new PodSetting
                {
                    ServerCount = ServerNum,
                    ClientCount = ClientNum
                }
            };
            Console.WriteLine(JsonConvert.SerializeObject(testJob));
            return testJob;
        }

        public List<TestConfigEntity> GenerateTestConfigs(string dir, string units)
        {
            var configs = new List<TestConfigEntity>();
            foreach (var u in units.Split(",").Select(int.Parse))
            {
                if (u != 1 && u != 2 && u != 5 && u != 10 && u != 20 && u != 50 && u != 100)
                {
                    throw new Exception($"Unit:{u} is not supported");
                }

                if (SignalRUnitSize != 1)
                {
                    throw new Exception("Template should be unit 1");
                }
                var config = Copy();
                config.PartitionKey =  config.PartitionKey +"-u" + u;
                config.RowKey = config.PartitionKey;
                config.SignalRUnitSize = u;
                config.ClientCons *= u;
                config.Start *= u;
                config.End *= u;
                config.Rate = Unit2Rate(u);
                config.Dir = dir;
                config.ClientNum = 0;
                config.ServerNum = 0;
                config.Init();
                configs.Add(config);
            }
            return configs;
        }

        private TestConfigEntity Copy()
        {
            return new TestConfigEntity()
            {
                PartitionKey = PartitionKey,
                RowKey = RowKey,
                User = User,
                ClientCons = ClientCons,
                ClientNum = ClientNum,
                ConnectEstablishRoundNum = ConnectEstablishRoundNum,
                ConnectionString = ConnectionString,
                Cron = Cron,
                Dir = Dir,
                End = End,
                Env = Env,
                Service = Service,
                Mode = Mode,
                Framework = Framework,
                SignalRUnitSize = SignalRUnitSize,
                ServerNum = ServerNum,
                InstanceIndex = InstanceIndex,
                Start = Start,
                RoundNum = RoundNum,
                GroupSize = GroupSize,
                Scenario = Scenario,
                RoundDurations = RoundDurations,
                Interval = Interval,
                MessageSize = MessageSize,
                Protocol = Protocol,
                Rate = Rate,
                LastCronTime = LastCronTime,
                Tags = Tags
            };
        }

        private static int Unit2Rate(int unit)
        {
            return unit switch
            {
                1 => 200,
                2 => 250,
                5 => 300,
                10 => 400,
                20 => 500,
                50 => 800,
                100 => 1600,
                _ => 200
            };
        }
    }
}