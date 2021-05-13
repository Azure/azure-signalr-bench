using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Messaging.WebPubSub;
using Azure.SignalRBench.Common;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace WpsUpstreamServer
{
    public class CloudEventMiddleware
    {
        private readonly RequestDelegate _nextMiddleware;
        private readonly WebPubSubServiceClient _client;

        public CloudEventMiddleware(RequestDelegate nextMiddleware, ServiceClientHolder serviceClientHolder)
        {
            _client = serviceClientHolder.WebPubSubServiceClient;
            _nextMiddleware = nextMiddleware;
        }

        public async Task Invoke(HttpContext context)
        {
            var headers = context.Request.Headers;
            if (headers.ContainsKey("ce-type"))
            {
                //Just echo back
                var response = context.Response;
                response.ContentType = context.Request.ContentType;
                using (StreamReader stream = new StreamReader(context.Request.Body))
                {
                    var body = await stream.ReadToEndAsync();
                    var data = JsonConvert.DeserializeObject<RawWebsocketData>(body);
                    switch (data.Type)
                    {
                        case "echo":
                            response.StatusCode = 200;
                             await context.Response.WriteAsync(body);
                             await context.Response.CompleteAsync();
                            break;
                        case "p2p":
                            response.StatusCode = 204;
                            response.ContentLength = 0;
                            await context.Response.CompleteAsync();
                            _= _client.SendToUserAsync(data.Target,body);
                            break;
                        case "broadcast":
                            response.StatusCode = 204;
                            response.ContentLength = 0;
                            await context.Response.CompleteAsync();
                            //async methods have bugs
                            _client.SendToAll(body);
                            break;
                    }
                }
                return;
            }
            await _nextMiddleware(context);
        }
    }
}