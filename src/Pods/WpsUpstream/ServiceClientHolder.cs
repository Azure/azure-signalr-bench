using System;
using System.Security.Claims;
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
            string key=null, endpoint=null;
            foreach (var property in properties)
            {
                if (property.StartsWith("Endpoint"))
                {
                    endpoint = property.Split("Endpoint=")[1];
                }
                else if (property.StartsWith("AccessKey"))
                    key = property.Split("AccessKey=")[1];
            }

            if( endpoint == null || key == null) throw new Exception($"can't parse connectionString:{connectionString}");
            WebPubSubServiceClient = new WebPubSubServiceClient(new Uri(endpoint), PerfConstants.Name.HubName, new Azure.AzureKeyCredential(key));
        }
    }
}