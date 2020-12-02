using System;
using System.Collections.Generic;
using Azure.SignalRBench.Common;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Azure.SignalRBench.Coordinator.Entities
{
    public class TestConfigEntity :TableEntity
    {
        public string? User { get; set; }
        public int ClientCons { get; set; } = 3000;

        public string? ConnectionString { get; set; } 
        public int SignalRUnitSize { get; set; }

        public int ServerNum { get; set; } = 1;

        public int InstanceIndex { get; set; } = 0;

        public int Start { get; set; } = 1;

        public int Step { get; set; } = 5;

        public int End { get; set; } =-1;
        
        public string Scenario { get; set; } = ClientBehavior.Echo.ToString();

        //seconds
        public int RoundDurations { get; set; } = 60;
        public int Interval { get; set; } = 1000;

        public int MessageSize { get; set; } = 2048;
        
        public string Protocol { get; set; } = SignalRProtocol.WebSocketsWithJson.ToString();

        public int Rate { get; set; } = 200;

        public void Init()
        {
            End = End <Start ? Start : End;
        }
        
        public TestJob ToTestJob(int index)
        {
            //creating round settings
            var roundsettings = new List<RoundSetting>();
            for (int i = Start; i <= End; i += Step)
            {
                roundsettings.Add(new RoundSetting()
                {
                    DurationInSeconds = RoundDurations,
                   ClientSettings = new []{new  ClientSetting()
                    {
                        Behavior=Enum.TryParse(Scenario,out ClientBehavior behavior)?behavior:throw new Exception($"Unknown Scenario {Scenario}"),
                        IntervalInMilliseconds=Interval,
                        Count=i,
                        MessageSize=MessageSize,
                        GroupFamily=null,
                    }
                   }
                });
            }
            var testJob = new TestJob()
            {
                TestId = PartitionKey + '-' + index,
                TestMethod = TestCategory.AspnetCore,
                ServiceSetting = new [] { new ServiceSetting()
                {
                    AsrsConnectionString = ConnectionString?.Trim(),
                    Location = "eastus",
                    Tier = "standard",
                    Size = SignalRUnitSize,
                } },
                ScenarioSetting = new ScenarioSetting()
                {
                    TotalConnectionCount = ClientCons,
                    Rounds = roundsettings.ToArray(),
                    IsAnonymous = true,
                    Protocol =Enum.TryParse(Protocol,out  SignalRProtocol protocol)?protocol:throw new Exception($"Unknown Protocol {Protocol}"),
                    Rate = Rate,
                },
                ServerSetting=new ServerSetting()
                {
                    ServerCount=ServerNum
                }
            };
            Console.WriteLine(JsonConvert.SerializeObject(testJob));
            return testJob;
        }
    }
}
