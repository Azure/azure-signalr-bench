﻿using Common;
using Plugin.Base;
using Plugin.Microsoft.Azure.SignalR.Benchmark.AgentMethods.Statistics;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.AgentMethods
{
    public class Wait : IAgentMethod
    {
        public async Task<IDictionary<string, object>> Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Wait...");

                // Get parameters
                stepParameters.TryGetTypedValue(SignalRConstants.Duration, out long duration, Convert.ToInt64);
                stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);

                await Task.Delay(TimeSpan.FromMilliseconds(duration));

                return null;
            }
            catch (Exception ex)
            {
                var message = $"Fail to wait: {ex}";
                Log.Error(message);
                throw;
            }
        }
    }
}
