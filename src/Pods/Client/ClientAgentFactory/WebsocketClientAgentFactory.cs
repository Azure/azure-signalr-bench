using System;
using System.Linq;
using System.Security.Claims;
using Azure.Messaging.WebPubSub;
using Azure.SignalRBench.Common;

namespace Azure.SignalRBench.Client
{
    public class WebsocketClientAgentFactory : IClientAgentFactory
    {
        public IClientAgent Create(string connectionString, Protocol protocol, string[] groups, int globalIndex,
            ClientAgentContext context)
        {
            //the app server url is hacked into this url using "," appended
            var urls = connectionString.Split(",");
            if (!TryParseEndpoint(urls[0], out var endpoint, out var key))
            {
                throw new Exception($"Fail to parse wps connection string:{connectionString}");
            }

            var uri = Uri(endpoint.Replace("http", "ws"), key, globalIndex);
            return new WebSocketClientAgent(
                uri.AbsoluteUri, urls[1], protocol,
                groups,
                globalIndex,
                context);
        }

        private bool TryParseEndpoint(string connectionString, out string endpoint, out string key)
        {
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
            var token = serviceClient.GenerateClientAccessUri(TimeSpan.FromHours(10),userId.ToString(),
                new[] {"webpubsub.sendToGroup", "webpubsub.joinLeaveGroup"});
            return token;
        }
    }
}