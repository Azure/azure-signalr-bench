﻿using Plugin.Base;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public class Echo : BaseContinuousSendMethod, ISlaveMethod
    {
        public Task<IDictionary<string, object>> Do(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters)
        {
            return SimpleScenarioSend(stepParameters, pluginParameters, SignalRConstants.EchoCallbackName);
        }
    }
}