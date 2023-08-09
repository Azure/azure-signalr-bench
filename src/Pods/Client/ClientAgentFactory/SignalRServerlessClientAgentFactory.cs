using Azure.SignalRBench.Client.ClientAgent;
using Azure.SignalRBench.Common;

namespace Azure.SignalRBench.Client.ClientAgentFactory
{
    public class SignalRServerlessClientAgentFactory : IClientAgentFactory
    {
        public IClientAgent Create(ClientAgentConfig config,
            ClientAgentContext context)
        {
            return new SignalRServerlessClientAgent(config.Url + NameConverter.GenerateHubName(context.TestId), config.Protocol, $"user{config.GlobalIndex}", config.Groups,
                config.GlobalIndex,
                context);
        }
    }
}