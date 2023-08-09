using Azure.SignalRBench.Client.ClientAgent;
using Azure.SignalRBench.Common;

namespace Azure.SignalRBench.Client.ClientAgentFactory
{
    public class SignalRDefaultClientAgentFactory : IClientAgentFactory
    {
        public IClientAgent Create(ClientAgentConfig config,
            ClientAgentContext context)
        {
            return new SignalRDefaultClientAgent(config.Url + PerfConstants.Name.HubName, config.Protocol, $"user{config.GlobalIndex}", config.Groups,
                config.GlobalIndex,
                context);
        }
    }
}