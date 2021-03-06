﻿using Plugin.Base;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Plugin.Microsoft.Azure.SignalR.Benchmark.SignalREnums;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.AgentMethods
{
    public class CreateConnection : IAgentMethod
    {
        public async Task<IDictionary<string, object>> Do(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Create connections...");
                if (SignalRUtils.isUsingInternalApp(stepParameters))
                {
                    // Setup the internal app server for simple local perf test
                    await SignalRUtils.StartInternalAppServer(stepParameters, pluginParameters);
                    // rewrite the url
                    stepParameters[SignalRConstants.HubUrls] = SignalRConstants.LocalhostUrl;
                }
                
                SignalRUtils.AgentCreateConnection(stepParameters, pluginParameters, ClientType.AspNetCore);
                return null;
            }
            catch (Exception ex)
            {
                var message = $"Fail to create connections: {ex}";
                Log.Error(message);
                throw;
            }
        }
    }
}
