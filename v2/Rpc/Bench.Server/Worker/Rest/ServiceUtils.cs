// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;

namespace Bench.RpcSlave.Worker.Serverless
{
    public class ServiceUtils
    {
        private static readonly JwtSecurityTokenHandler JwtTokenHandler = new JwtSecurityTokenHandler();
        private const string EndpointProperty = "endpoint";
        private const string AccessKeyProperty = "accesskey";
        private const string VersionProperty = "version";
        private const string PortProperty = "port";
        // For SDK 1.x, only support Azure SignalR Service 1.x
        private const string SupportedVersion = "1";
        private const string ValidVersionRegex = "^" + SupportedVersion + @"\.\d+(?:[\w-.]+)?$";

        private static readonly string MissingRequiredProperty =
            $"Connection string missing required properties endpoint and accesskey.";

        private const string InvalidVersionValueFormat = "Version {0} is not supported.";

        private static readonly string InvalidPortValue = $"Invalid value for {PortProperty} property.";

        private static readonly char[] PropertySeparator = { ';' };
        private static readonly char[] KeyValueSeparator = { '=' };
        public const string ClientUserIdPrefix = "cli";
        public const string MethodName = "SendMessage";
        public const string HubName = "RestBench";

        public string Endpoint { get; }

        public string AccessKey { get; }

        public int? Port { get; }

        public ServiceUtils(string connectionString)
        {
            (Endpoint, AccessKey, _, Port) = ParseConnectionString(connectionString);
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

        public string GetBroadcastUrl()
        {
            return $"{GetBaseUrl(HubName)}";
        }

        public string GetSendToUserUrl(string hubName, string userId)
        {
            return $"{GetBaseUrl(hubName)}/users/{userId}";
        }

        private string GetBaseUrl(string hubName)
        {
            return $"{Endpoint}/api/v1/hubs/{hubName.ToLower()}";
        }

        public static string GenerateServerName()
        {
            return $"{Environment.MachineName}_{Guid.NewGuid():N}";
        }

        public string GetClientUrl()
        {
            return Port.HasValue ?
                $"{Endpoint}:{Port}/client/?hub={HubName}" :
                $"{Endpoint}/client/?hub={HubName}";
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

        internal static (string endpoint, string accessKey, string version, int? port) ParseConnectionString (string connectionString)
        {
            var properties = connectionString.Split(PropertySeparator, StringSplitOptions.RemoveEmptyEntries);
            if (properties.Length < 2)
            {
                throw new ArgumentException(MissingRequiredProperty, nameof(connectionString));
            }

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

            if (!dict.ContainsKey(EndpointProperty) || !dict.ContainsKey(AccessKeyProperty))
            {
                throw new ArgumentException(MissingRequiredProperty, nameof(connectionString));
            }

            if (!ValidateEndpoint(dict[EndpointProperty]))
            {
                throw new ArgumentException($"Endpoint property in connection string is not a valid URI: {dict[EndpointProperty]}.");
            }

            string version = null;
            if (dict.TryGetValue(VersionProperty, out var v))
            {
                if (Regex.IsMatch(v, ValidVersionRegex))
                {
                    version = v;
                }
                else
                {
                    throw new ArgumentException(string.Format(InvalidVersionValueFormat, v), nameof(connectionString));
                }
            }

            int? port = null;
            if (dict.TryGetValue(PortProperty, out var s))
            {
                if (int.TryParse(s, out var p) &&
                    p > 0 && p <= 0xFFFF)
                {
                    port = p;
                }
                else
                {
                    throw new ArgumentException(InvalidPortValue, nameof(connectionString));
                }
            }

            return (dict[EndpointProperty].TrimEnd('/'), dict[AccessKeyProperty], version, port);
        }

        internal static bool ValidateEndpoint(string endpoint)
        {
            return Uri.TryCreate(endpoint, UriKind.Absolute, out var uriResult) &&
                   (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

    }
}
