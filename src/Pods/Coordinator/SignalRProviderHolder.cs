using System;
using Azure.SignalRBench.Common;

namespace Azure.SignalRBench.Coordinator
{
    public class SignalRProviderHolder
    {
        public ISignalRProvider? PPE { get; set; }
        public  ISignalRProvider AzureGlobal { get;set; }

        public ISignalRProvider GetSignalRProvider(string env)
        {
            if (env == PerfConstants.Cloud.AzureGlobal)
            {
                return AzureGlobal;
            }

            if (env == PerfConstants.Cloud.PPE)
            {
                return PPE??throw new Exception("PPE not supported");
            }
            throw new Exception("Not supported env");
        }
    }
}