// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Bench.RpcSlave.Worker.Serverless
{
    public class ServiceUtils
    {
        private static readonly JwtSecurityTokenHandler JwtTokenHandler = new JwtSecurityTokenHandler();

        public const string ClientUserIdPrefix = "cli";
        public const string MethodName = "SendMessage";
        public const string HubName = "RestBench";

        public string Endpoint { get; }

        public string AccessKey { get; }

        public ServiceUtils(string connectionString)
        {
            (Endpoint, AccessKey) = ParseConnectionString(connectionString);
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

            return GenerateAccessTokenInternal(audience, claims, lifetime ?? TimeSpan.FromHours(1));
        }

        public static string GetClientUrl(string endpoint, string hubName)
        {
            return $"{endpoint}:5001/client/?hub={hubName}";
        }

        public string GetSendToUserUrl(string hubName, string userId)
        {
            return $"{GetBaseUrl(hubName)}/user/{userId}";
        }

        private string GetBaseUrl(string hubName)
        {
            return $"{Endpoint}:5002/api/v1-preview/hub/{hubName.ToLower()}";
        }

        public static string GenerateServerName()
        {
            return $"{Environment.MachineName}_{Guid.NewGuid():N}";
        }

        public string GetClientUrl()
        {
            return GetClientUrl(Endpoint, HubName);
        }

        public string GenerateAccessTokenInternal(string audience, IEnumerable<Claim> claims, TimeSpan lifetime)
        {
            var expire = DateTime.UtcNow.Add(lifetime);

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AccessKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = JwtTokenHandler.CreateJwtSecurityToken(
                issuer: null,
                audience: audience,
                subject: claims == null ? null : new ClaimsIdentity(claims),
                expires: expire,
                signingCredentials: credentials);
            return JwtTokenHandler.WriteToken(token);
        }

        private static readonly char[] PropertySeparator = { ';' };
        private static readonly char[] KeyValueSeparator = { '=' };
        private const string EndpointProperty = "endpoint";
        private const string AccessKeyProperty = "accesskey";

        internal static (string, string) ParseConnectionString(string connectionString)
        {
            var properties = connectionString.Split(PropertySeparator, StringSplitOptions.RemoveEmptyEntries);
            if (properties.Length > 1)
            {
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var property in properties)
                {
                    var kvp = property.Split(KeyValueSeparator, 2);
                    if (kvp.Length != 2) continue;

                    var key = kvp[0].Trim();
                    if (dict.ContainsKey(key))
                    {
                        throw new ArgumentException($"Duplicate properties found in connection string: {key}.");
                    }

                    dict.Add(key, kvp[1].Trim());
                }

                if (dict.ContainsKey(EndpointProperty) && dict.ContainsKey(AccessKeyProperty))
                {
                    return (dict[EndpointProperty].TrimEnd('/'), dict[AccessKeyProperty]);
                }
            }

            throw new ArgumentException($"Connection string missing required properties {EndpointProperty} and {AccessKeyProperty}.");
        }
    }
}
