using System;
using Azure.SignalRBench.Common;

namespace Azure.SignalRBench.Coordinator
{
    public class SignalRProvider
    {
        public ISignalRServiceManagement? PPE { get; set; }
        public ISignalRServiceManagement AzureGlobal { get; set; }

        public ISignalRServiceManagement GetSignalRProvider(string env)
        {
            if (env == PerfConstants.Cloud.AzureGlobal)
            {
                return AzureGlobal;
            }

            if (env == PerfConstants.Cloud.PPE)
            {
                return PPE ?? throw new Exception("PPE not supported");
            }

            throw new Exception("Not supported env");
        }
    }
}