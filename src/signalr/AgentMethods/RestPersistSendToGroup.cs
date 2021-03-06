﻿using Microsoft.Azure.SignalR.Management;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.AgentMethods
{
    public class RestPersistSendToGroup : RestGroupBase
    {
        protected override Task<IServiceHubContext> CreateHubContextAsync()
        {
            return CreateHubContextHelperAsync(ServiceTransportType.Persistent);
        }
    }
}
