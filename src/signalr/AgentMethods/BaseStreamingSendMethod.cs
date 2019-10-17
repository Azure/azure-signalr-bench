using Common;
using System;
using System.Collections.Generic;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.AgentMethods
{
    public class BaseStreamingSendMethod : BaseContinuousSendMethod
    {
        protected int StreamItemsCount = 1;
        protected int StreamItemInterval = 0;

        protected override void LoadParametersAndContext(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters)
        {
            base.LoadParametersAndContext(stepParameters, pluginParameters);
            stepParameters.TryGetTypedValue(SignalRConstants.StreamingItemCount, out StreamItemsCount, Convert.ToInt32);
            stepParameters.TryGetTypedValue(SignalRConstants.StreamingItemInterval, out StreamItemInterval, Convert.ToInt32);
        }
    }
}
