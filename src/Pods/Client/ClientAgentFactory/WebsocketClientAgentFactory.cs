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
            if (!TryParseEndpoint(connectionString, out var endpoint, out var key))
            {
                throw new Exception($"Fail to parse wps connection string:{connectionString}");
            }

            var token = Token(endpoint, key, globalIndex);
            return new WebSocketClientAgent(
                endpoint.Replace("http", "ws") + "/client/hubs/" + PerfConstants.Name.HubName + "?access_token=" + token, protocol,
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

        private static string Token(string endpoint, string key, int userId)
        {
            var serviceClient = new WebPubSubServiceClient(new Uri(endpoint), PerfConstants.Name.HubName, new Azure.AzureKeyCredential(key));
            var sendToGroupRole = new Claim("role", "webpubsub.sendToGroup");
            var joinLeaveGroupRole = new Claim("role", "webpubsub.joinLeaveGroup");
            var sub = new Claim("sub", "user"+userId.ToString());
            var token = serviceClient.GetClientAccessToken(TimeSpan.FromHours(10),
                new[] {sendToGroupRole, joinLeaveGroupRole,sub});
            return token;
        }
    }
}