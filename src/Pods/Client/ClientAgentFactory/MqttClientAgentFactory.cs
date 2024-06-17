using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Azure.SignalRBench.Client.ClientAgent;
using Azure.SignalRBench.Common;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace Azure.SignalRBench.Client.ClientAgentFactory
{
    public class MqttClientAgentFactory : WebsocketClientAgentFactory
    {

        public MqttClientAgentFactory(ILoggerFactory loggerFactory) : base(loggerFactory)
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

            var uri = Path(endpoint.Replace("http", "ws"), key, config.GlobalIndex);
            return new MqttClientAgent(
                uri, urls[1], 
                config.Groups,
                config.GlobalIndex,
                config.PublishQos,
                config.SubscribeQos,
                context,
                LoggerFactory);
        }
        
        protected static string Path(string endpoint, string accessKey, int userId)
        {
            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(accessKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var url = $"{endpoint}/clients/mqtt/hubs/{PerfConstants.Name.HubName}";
            
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Aud, url),
                new Claim(JwtRegisteredClaimNames.Exp, ((DateTimeOffset)DateTime.UtcNow.AddHours(1)).ToUnixTimeSeconds().ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())
            };

            claims.AddRange(new[] {"webpubsub.sendToGroup", "webpubsub.joinLeaveGroup"}.Select(role => new Claim("roles", role)));
            var token = new JwtSecurityToken(
                claims: claims,
                signingCredentials: creds
            );

            var tokenString= new JwtSecurityTokenHandler().WriteToken(token);
            return $"{endpoint}:443/clients/mqtt/hubs/{PerfConstants.Name.HubName}?access_token={tokenString}";
        }
    }
}