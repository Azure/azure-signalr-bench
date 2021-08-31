using Azure.SignalRBench.Client.ClientAgent;
using Azure.SignalRBench.Common;

namespace Azure.SignalRBench.Client.ClientAgentFactory
{
    public class AspNetSignalRClientAgentFactory : IClientAgentFactory
    {
        public IClientAgent Create(string url, Protocol protocol, string[] groups, int globalIndex,
            ClientAgentContext context)
        {
            return new AspNetSignalRClientAgent(url ,PerfConstants.Name.HubName, protocol, $"user{globalIndex}", groups,
                globalIndex,
                context);
        }
    }
}