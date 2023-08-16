using System;
using Azure.SignalRBench.Client.ClientAgent;
using Microsoft.Extensions.Logging;

namespace Azure.SignalRBench.Client.ClientAgentFactory
{
    public class SioClientAgentFactory : WebsocketClientAgentFactory
    {

        public SioClientAgentFactory(ILoggerFactory loggerFactory) : base(loggerFactory)
        {
        }

        public  override IClientAgent Create(ClientAgentConfig config,
            ClientAgentContext context)
        {
            if (!TryParseEndpoint(config.Url, out var endpoint, out var _))
            {

                throw new Exception($"Fail to parse socketIO connection string:{config.Url}");
            }
            return new SioClientAgent(endpoint, context, config.Groups, config.GlobalIndex,
                config.ClientExpectServerAck, config.ServerExpectClientAck,
                LoggerFactory);
        }
    }
}