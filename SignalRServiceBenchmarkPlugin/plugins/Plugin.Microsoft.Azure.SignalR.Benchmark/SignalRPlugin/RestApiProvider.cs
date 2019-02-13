using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    public class RestApiProvider : ConnectionStringParser
    {
        private static JwtSecurityTokenHandler JwtTokenHandler { get; } = new JwtSecurityTokenHandler();
        private readonly string _baseEndpoint;
        private readonly string _hubName;
        private readonly string _commonPrefix;
        private readonly string _accessKey;

        public RestApiProvider(string connectionString, string hubName)
        {
            (_baseEndpoint, _accessKey, _, Port) = Parse(connectionString);
            _commonPrefix = $"{_baseEndpoint}/api/v1/hubs/{_hubName}";
        }

        public string GetClientUrl()
        {
            return Port.HasValue ?
                $"{_baseEndpoint}:{Port}/client/?hub={_hubName}" :
                $"{_baseEndpoint}/client/?hub={_hubName}";
        }

        public string GenerateAccessToken(string audience, string userId, TimeSpan? lifetime = null)
        {
            IEnumerable<Claim> claims = null;
            if (userId != null)
            {
                claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                };
            }

            return GenerateAccessTokenInternal(audience, claims, lifetime ?? TimeSpan.FromHours(5));
        }

        private string GenerateAccessTokenInternal(string audience, IEnumerable<Claim> claims, TimeSpan lifetime)
        {
            var expire = DateTime.UtcNow.Add(lifetime);

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_accessKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = JwtTokenHandler.CreateJwtSecurityToken(
                issuer: null,
                audience: audience,
                subject: claims == null ? null : new ClaimsIdentity(claims),
                expires: expire,
                signingCredentials: credentials);
            return JwtTokenHandler.WriteToken(token);
        }
    }
}
