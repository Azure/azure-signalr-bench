using Azure.SignalRBench.Common;

namespace Azure.SignalRBench.Client
{
    public interface IClientAgentFactory
    {
        IClientAgent Create(string url, Protocol protocol, string[] groups, int globalIndex,
            ClientAgentContext context);
    }
}