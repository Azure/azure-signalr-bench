using Azure.SignalRBench.Client.ClientAgent;
using Azure.SignalRBench.Common;

namespace Azure.SignalRBench.Client.ClientAgentFactory
{
    public class AspNetSignalRClientAgentFactory : IClientAgentFactory
    {
        public IClientAgent Create(ClientAgentConfig config,
            ClientAgentContext context)
        {
            return new AspNetSignalRClientAgent(config.Url ,PerfConstants.Name.HubName, config.Protocol, $"user{config.GlobalIndex}", config.Groups,
                config.GlobalIndex,
                context);
        }
    }
}