﻿using System;
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

        public int ConnectEstablishRoundNum { get; set; } = 1;

        public string? ConnectionString { get; set; } 
        public int SignalRUnitSize { get; set; }

        public int ServerNum { get; set; } = -1;

        public int ClientNum { get; set; } = -1;

        public int InstanceIndex { get; set; } = 0;

        public int Start { get; set; } = 1;

        public int RoundNum { get; set; } = 5;

        public int End { get; set; } =-1;

        public int GroupSize { get; set; } = 100;
        
        public string Scenario { get; set; } = ClientBehavior.Echo.ToString();

        //seconds
        public int RoundDurations { get; set; } = 60;
        public int Interval { get; set; } = 1000;

        public int MessageSize { get; set; } = 2048;
        
        public string Protocol { get; set; } = SignalRProtocol.WebSocketsWithJson.ToString();

        public int Rate { get; set; } = 200;

        public void Init()
        {
            Start = Start > ClientCons ? ClientCons : Start;
            End = End > ClientCons ? ClientCons : End;
            End = End <Start ? Start : End;
            if (ConnectEstablishRoundNum < 1)
                ConnectEstablishRoundNum = 1;
            if (ConnectEstablishRoundNum > RoundNum)
                ConnectEstablishRoundNum = RoundNum;
            if (ClientNum <= 0)
            {
                ClientNum = (int) Math.Ceiling((double) ClientCons / PerfConstants.Number.ConnectionsPerClient);
            }

            if (ServerNum <= 0)
            {
                ServerNum = (int) Math.Ceiling((double)ClientNum/2);
            }

            if (RoundNum <= 0)
            {
                RoundNum = 5;
            }
        }
        
        public TestJob ToTestJob(int index)
        {
            //creating round settings
            var roundsettings = new List<RoundSetting>();
            int current = Start;
            int step =RoundNum>1?(int)Math.Ceiling((double)(End - Start)/(RoundNum-1)):0;
            int count = current;
            var Behavior = Enum.TryParse(Scenario, out ClientBehavior behavior)
                ? behavior
                : throw new Exception($"Unknown Scenario {Scenario}");
            for (int i = 0; i < RoundNum; i++)
            {
                count = current > End ? End : current;
                roundsettings.Add(new RoundSetting()
                {
                    DurationInSeconds = RoundDurations,
                    ClientSettings = new []{new  ClientSetting()
                        {
                            Behavior=Behavior,
                            IntervalInMilliseconds=Interval,
                            Count=count,
                            MessageSize=MessageSize,
                            GroupFamily="default",
                        }
                    }
                });
                current += step ;
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
                    TotalConnectionRound = ConnectEstablishRoundNum,
                    Rounds = roundsettings.ToArray(),
                    IsAnonymous = true,
                    Protocol =Enum.TryParse(Protocol,out  SignalRProtocol protocol)?protocol:throw new Exception($"Unknown Protocol {Protocol}"),
                    Rate = Rate,
                    GroupDefinitions = (Behavior==ClientBehavior.GroupBroadcast) ? new []{new GroupDefinition()
                    {
                        GroupFamily = "default",
                        GroupCount = 0,
                        GroupSize = GroupSize
                    } }: Array.Empty<GroupDefinition>(),
                },
                PodSetting=new PodSetting()
                {
                    ServerCount= ServerNum,
                    ClientCount = ClientNum
                },
               
            };
            Console.WriteLine(JsonConvert.SerializeObject(testJob));
            return testJob;
        }
    }
}
