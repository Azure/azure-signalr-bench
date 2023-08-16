using Azure.SignalRBench.Client.ClientAgent;
using Azure.SignalRBench.Common;

namespace Azure.SignalRBench.Client
{
    public interface IClientAgentFactory
    {
        IClientAgent Create(ClientAgentConfig clientAgentConfig, ClientAgentContext context);
    }
}