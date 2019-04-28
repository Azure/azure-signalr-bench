using Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    internal class PayloadMessage
    {
        public string Target { get; set; }

        public object[] Arguments { get; set; }
    }

    internal static class HttpClientFactory
    {
        private static readonly IHttpClientFactory _clientFactory;

        static HttpClientFactory()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient();
            var services = serviceCollection.BuildServiceProvider();

            _clientFactory = services.GetRequiredService<IHttpClientFactory>();
        }

        public static HttpClient CreateClient()
        {
            return _clientFactory.CreateClient();
        }
    }

    public class RestApiProvider : ConnectionStringParser
    {
        private static JwtSecurityTokenHandler JwtTokenHandler { get; } = new JwtSecurityTokenHandler();
        private readonly string _baseEndpoint;
        private readonly string _hubName;
        private readonly string _urlCommonPrefix;
        private readonly string _audienceCommonPrefix;
        private readonly string _accessKey;

        public RestApiProvider(string connectionString, string hubName)
        {
            _hubName = hubName;
            (_baseEndpoint, _accessKey, _, Port) = Parse(connectionString);
            var postfix = $"/api/v1/hubs/{_hubName}";
            _audienceCommonPrefix = $"{_baseEndpoint}{postfix}";
            _urlCommonPrefix = Port.HasValue ?
                $"{_baseEndpoint}:{Port}{postfix}" :
                _audienceCommonPrefix;   
        }

        public string GetClientUrl()
        {
            return Port.HasValue ?
                $"{_baseEndpoint}:{Port}/client/?hub={_hubName}" :
                $"{_baseEndpoint}/client/?hub={_hubName}";
        }

        public string GetClientAudience()
        {
            return $"{_baseEndpoint}/client/?hub={_hubName}";
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

        public async Task SendToUser(
            string userId,
            string methodName,
            object[] args,
            CancellationToken cancellationToken = default)
        {
            var sendToUserEndpoint = $"{_urlCommonPrefix}/users/{userId}";
            var sendToUserAudience = $"{_audienceCommonPrefix}/users/{userId}";
            var token = GenerateAccessToken(sendToUserAudience, userId);
            using (var request = BuildHttpRequest(sendToUserEndpoint, token, methodName, args))
            {
                await SendHttpRequestAsync(request, cancellationToken);
            }
        }

        public async Task SendToAll(
            string methodName,
            object[] args,
            CancellationToken cancellationToken = default)
        {
            var broadcastEndpoint = _urlCommonPrefix;
            var broadcastAudience = _audienceCommonPrefix;
            var token = GenerateAccessToken(broadcastAudience, Util.GenerateServerName());
            using (var request = BuildHttpRequest(broadcastEndpoint, token, methodName, args))
            {
                await SendHttpRequestAsync(request, cancellationToken);
            }
        }

        private async Task SendHttpRequestAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken = default)
        {
            var httpClient = HttpClientFactory.CreateClient();
            using (var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
            }
        }

        private HttpRequestMessage BuildHttpRequest(
            string url,
            string token,
            string methodName,
            object[] args)
        {
            var payload = new PayloadMessage
            {
                Target = methodName,
                Arguments = args
            };
            var request = new HttpRequestMessage(HttpMethod.Post, GetUrl(url));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            return request;
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

        private Uri GetUrl(string baseUrl)
        {
            return new UriBuilder(baseUrl).Uri;
        }
    }
}
