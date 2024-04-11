using System;
using Azure.Messaging.WebPubSub;
using Azure.SignalRBench.Client.ClientAgent;
using Azure.SignalRBench.Common;
using Microsoft.Extensions.Logging;

namespace Azure.SignalRBench.Client.ClientAgentFactory
{
    public class WebsocketPythonServerClientAgentFactory : WebsocketClientAgentFactory
    {
        public WebsocketPythonServerClientAgentFactory(ILoggerFactory loggerFactory) : base(loggerFactory)
        {
        }

        public override IClientAgent Create(ClientAgentConfig config,
            ClientAgentContext context)
        {
            //the app server url is hacked into this url using "," appended
            var urls = config.Url.Split(",");
            if (!TryParseEndpoint(urls[0], out var endpoint, out var key))
            {
                throw new Exception($"Fail to parse wps connection string:{config.Url}");
            }

            var uri = Uri(endpoint.Replace("http", "ws"), key, config.GlobalIndex);
            return new WebSocketPythonServerClientAgent(
                uri.AbsoluteUri, urls[1], config.Protocol,
                config.Groups,
                config.GlobalIndex,
                context,
                LoggerFactory);
        }
    }
}