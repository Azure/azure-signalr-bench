using Azure.SignalRBench.Common;

namespace Azure.SignalRBench.Client
{
    public class SignalRServerlessClientAgentFactory : IClientAgentFactory
    {
        public IClientAgent Create(string url, Protocol protocol, string[] groups, int globalIndex,
            ClientAgentContext context)
        {
            return new SignalRServerlessClientAgent(url + NameConverter.GenerateHubName(context.TestId), protocol, $"user{globalIndex}", groups,
                globalIndex,
                context);
        }
    }
}