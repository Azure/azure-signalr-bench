using System;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Messaging.WebPubSub;
using Azure.SignalRBench.Common;

namespace WpsUpstreamServer
{
    public class ServiceClientHolder
    {
        public WebPubSubServiceClient WebPubSubServiceClient { get; }
        public ServiceClientHolder(string connectionString)
        {
            var properties = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            string key = null, endpoint = null;
            foreach (var property in properties)
            {
                if (property.StartsWith("Endpoint"))
                {
                    endpoint = property.Split("Endpoint=")[1];
                }
                else if (property.StartsWith("AccessKey"))
                    key = property.Split("AccessKey=")[1];
            }

            var httpClient = new HttpClient(new Http2MessageHandler());
            
            var options = new WebPubSubServiceClientOptions()
            {
                Transport = new HttpClientTransport(httpClient)
            };

            if (endpoint == null || key == null)
                throw new Exception($"can't parse connectionString:{connectionString}");
            WebPubSubServiceClient = new WebPubSubServiceClient(new Uri(endpoint), PerfConstants.Name.HubName,
                new Azure.AzureKeyCredential(key), options);
        }

        private class Http2MessageHandler : HttpClientHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                request.Version = new Version(1, 1);
                request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
                return base.SendAsync(request, cancellationToken);
            }
        }
    }
}