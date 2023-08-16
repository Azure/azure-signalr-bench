using System;
using Azure.Messaging.WebPubSub;
using Azure.SignalRBench.Client.ClientAgent;
using Azure.SignalRBench.Common;
using Microsoft.Extensions.Logging;

namespace Azure.SignalRBench.Client.ClientAgentFactory
{
    public class WebsocketClientAgentFactory : IClientAgentFactory
    {
        protected readonly ILoggerFactory LoggerFactory;
        private readonly ILogger<WebsocketClientAgentFactory> _logger;

        public WebsocketClientAgentFactory(ILoggerFactory loggerFactory)
        {
            LoggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<WebsocketClientAgentFactory>();
        }

        public virtual IClientAgent Create(ClientAgentConfig config,
            ClientAgentContext context)
        {
            //the app server url is hacked into this url using "," appended
            var urls = config.Url.Split(",");
            if (!TryParseEndpoint(urls[0], out var endpoint, out var key))
            {
                throw new Exception($"Fail to parse wps connection string:{config.Url}");
            }

            var uri = Uri(endpoint.Replace("http", "ws"), key, config.GlobalIndex);
            return new WebSocketClientAgent(
                uri.AbsoluteUri, urls[1], config.Protocol,
                config.Groups,
                config.GlobalIndex,
                context,
                LoggerFactory);
        }

        protected static bool TryParseEndpoint(string connectionString, out string endpoint, out string key)
        {
            var eps= connectionString.Split(" ");
            if (eps.Length > 1)
            {
                // multiple endpoint. Use for replica only for now
                connectionString = eps[StaticRandom.Next(eps.Length)];
            }
            var properties = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            endpoint = null;
            key = null;
            foreach (var property in properties)
            {
                if (property.StartsWith("Endpoint"))
                {
                    endpoint = property.Split("Endpoint=")[1];
                }
                else if (property.StartsWith("AccessKey"))
                    key = property.Split("AccessKey=")[1];
            }

            return endpoint != null && key != null;
        }

        private static Uri Uri(string endpoint, string key, int userId)
        {
            var serviceClient = new WebPubSubServiceClient(new Uri(endpoint), PerfConstants.Name.HubName, new AzureKeyCredential(key));
            var token = serviceClient.GenerateClientAccessUri(TimeSpan.FromHours(10), "user"+userId.ToString(),
                new[] {"webpubsub.sendToGroup", "webpubsub.joinLeaveGroup"});
            return token;
        }
    }
}