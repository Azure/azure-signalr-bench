using Azure.SignalRBench.Common;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Entities
{
    public class TestConfigEntity :TableEntity
    {

        public int ClientCons { get; set; }

        public int SignalRUnitSize { get; set; }

        public int ServerNum { get; set; }

        public TestJob ToTestJob()
        {
            var testJob = new TestJob()
            {
                TestId = PartitionKey + '-' + RowKey,
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
                    Rate = 1,
                }

            };
            return testJob;
        }
    }
}
