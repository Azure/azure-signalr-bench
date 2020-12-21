using Azure.SignalRBench.Common;

namespace Azure.SignalRBench.Client
{
    public class WebsocketClientAgentFactory:IClientAgentFactory
    {
        public IClientAgent Create(string url,Protocol protocol, string[] groups, int globalIndex,
            ClientAgentContext context)
        {
            return new WebSocketClientAgent(url, protocol, groups,
                globalIndex,
                context);
        }
    }
}