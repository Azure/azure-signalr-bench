﻿using System;
using Azure.SignalRBench.Common;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;

namespace Azure.SignalRBench.Coordinator.Entities
{
    public class TestConfigEntity :TableEntity
    {
        public int ClientCons { get; set; } = 3000;

        public int SignalRUnitSize { get; set; }

        public int ServerNum { get; set; } = 1;

        public int Index { get; set; } = 0;

        public int Start { get; set; } = 0;

        public int Step { get; set; } = 5;

        public int End { get; set; } = 10;

        public ClientBehavior Scenario { get; set; } = ClientBehavior.Echo;

        public int Interval { get; set; } = 1000;

        public int MessageSize { get; set; } = 1024;
        
        public SignalRProtocol Protocol { get; set; } = SignalRProtocol.WebSocketsWithJson;
        public TestJob ToTestJob(int index)
        {
            var testJob = new TestJob()
            {
                TestId = PartitionKey + '-' + index,
                TestMethod = TestCategory.AspnetCore,
                ServiceSetting = new ServiceSetting[] { new ServiceSetting()
                {
                    Location = "eastus",
                    Tier = "standard",
                    Size = SignalRUnitSize,
                } },
                ScenarioSetting = new ScenarioSetting()
                {
                    TotalConnectionCount = ClientCons,
                    Rounds = new RoundSetting[]
                    {
                        new RoundSetting()
                        {
                            DurationInMinutes=1,
                            ClientSettings=new ClientSetting[]
                            {
                                new ClientSetting()
                                {
                                    Behavior=ClientBehavior.Echo,
                                    IntervalInMilliseconds=1000,
                                    Count=10,
                                    MessageSize=1024,
                                    GroupFamily=null,
                                }
                            }
                        }
                    },
                    IsAnonymous = true,
                    Protocol = SignalRProtocol.WebSocketsWithJson,
                    Rate = 200,
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
