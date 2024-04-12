using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.WebPubSub.Clients;
using Azure.SignalRBench.Client.Protobuf;
using Azure.SignalRBench.Common;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Azure.SignalRBench.Client.ClientAgent
{
    class WebSocketPythonServerClientAgent : WebSocketClientAgent
    {
        
        public WebSocketPythonServerClientAgent(string url, string appserverUrl, Protocol protocol, string[] groups,
            int globalIndex,
            ClientAgentContext context,
            ILoggerFactory loggerFactory):base(url, appserverUrl, protocol, groups, globalIndex, context, loggerFactory)
        {
        }

        public override Task GroupBroadcastAsync(string group, string payload)
        {
            var data = new RawWebsocketData()
            {
                Type = "sendToGroup",
                Target = group,
                Ticks = ClientAgentContext.CoordinatedUtcNow(),
                Payload = payload
            };
            return SendToAppServer(data);
        }
    }
    
}