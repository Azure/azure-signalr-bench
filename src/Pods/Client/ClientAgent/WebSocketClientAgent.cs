using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.WebPubSub.Clients;
using Azure.SignalRBench.Common;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Azure.SignalRBench.Client.ClientAgent
{
    class WebSocketClientAgent : IClientAgent
    {
        public ClientAgentContext Context { get; }
        public int GlobalIndex { get; }
        public string[] Groups { get; }

        private WebSocketHubConnection Connection { get; }

        private readonly string _appserverUrl;
        private readonly ILogger<WebSocketClientAgent> _logger;
        private readonly AzureEventSourceLogForwarder _forwarder;

        private static readonly HttpClient HttpClient = new HttpClient();

        public WebSocketClientAgent(string url, string appserverUrl, Protocol protocol, string[] groups,
            int globalIndex,
            ClientAgentContext context,
            ILoggerFactory loggerFactory)
        {
            _forwarder = new AzureEventSourceLogForwarder(loggerFactory);
            _forwarder.Start();
            _logger = loggerFactory.CreateLogger<WebSocketClientAgent>();
            Context = context;
            _appserverUrl = "http://" + appserverUrl;
            Connection = new WebSocketHubConnection(url, this, protocol, context, _logger);
            Connection.On(context.Measure);
            Groups = groups;
            GlobalIndex = globalIndex;
        }
        
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            await Connection.StartAsync(cancellationToken);
            await Context.OnConnected(this, Groups.Length > 0);
        }
        
        public async Task JoinGroupAsync()
        {
            await Connection.JoinGroup(Groups[0]);
        }


        public async Task StopAsync()
        {
            await Connection.StopAsync();
        }

        //This method should be sent directly to appserver to lower pressure on wps runtime 
        public Task BroadcastAsync(string payload)
        {
            var data = new RawWebsocketData()
            {
                Type = "broadcast",
                Ticks = ClientAgentContext.CoordinatedUtcNow(),
                Payload = payload
            };
            return SendToAppServer(data);
        }

        public Task EchoAsync(string payload)
        {
            var data = new RawWebsocketData()
            {
                Type = "echo",
                Ticks = ClientAgentContext.CoordinatedUtcNow(),
                Payload = payload
            };
            return Connection.SendEventAsync(NameConverter.GenerateHubName(Context.TestId), data);
        }

        public Task GroupBroadcastAsync(string group, string payload)
        {
            var data = new RawWebsocketData()
            {
                Ticks = ClientAgentContext.CoordinatedUtcNow(),
                Payload = payload
            };
            return Connection.SendToGroup(group, data);
        }
        
        //This method is sent directly to appserver to lower pressure on wps runtime 
        public Task SendToClientAsync(int index, string payload)
        {
            var data = new RawWebsocketData()
            {
                Type = "p2p",
                Ticks = ClientAgentContext.CoordinatedUtcNow(),
                Payload = payload,
                Target = $"user{index}"
            };
            return SendToAppServer(data);
        }


        private sealed class WebSocketHubConnection
        {
            private readonly WebPubSubClient _socket;
            private readonly CancellationTokenSource _connectionStoppedCts = new CancellationTokenSource();
            private readonly WebSocketClientAgent _agent;
            private readonly ILogger _logger;

            private volatile bool _closed;
            private ClientAgentContext _context;
            public Uri ResourceUri { get; }

            public WebSocketHubConnection(string url, WebSocketClientAgent agent, Protocol protocol,
                ClientAgentContext context, ILogger logger)
            {
                ResourceUri = new Uri(url);
                switch (protocol)
                {
                    case Protocol.RawWebSocketReliableJson:
                        _socket = new WebPubSubClient(ResourceUri);
                        break;
                    case Protocol.RawWebSocketJson:
                        var options = new WebPubSubClientOptions();
                        options.Protocol = new WebPubSubJsonProtocol();
                        _socket = new WebPubSubClient(ResourceUri, options);
                        break;
                    default:
                        throw new Exception($"Unsupported protocol {protocol}");
                }
                _agent = agent;
                _context = context;
                _logger = logger;

                _socket.Disconnected += _ => { _closed = true; return _context.OnClosed(_agent); };
            }

            public void On(Action<long, string> callback)
            {
                _socket.ServerMessageReceived += e =>
                {
                    var data = e.Message.Data.ToObjectFromJson<RawWebsocketData>();
                    callback(data.Ticks, data.Payload);
                    return Task.CompletedTask;
                };
                _socket.GroupMessageReceived += e =>
                {
                    var data = e.Message.Data.ToObjectFromJson<RawWebsocketData>();
                    callback(data.Ticks, data.Payload);
                    return Task.CompletedTask;
                };
            }

            public volatile bool active = false;

            public Task JoinGroup(string group)
            {
                if (active && _closed)
                {
                    _context.OnClosed(_agent);
                    active = false;
                }

                return _socket.JoinGroupAsync(group);
            }

            public Task SendToGroup(string group, RawWebsocketData value)
            {
                if (active && _closed)
                {
                    _context.OnClosed(_agent);
                    active = false;
                }

                return _socket.SendToGroupAsync(group, BinaryData.FromObjectAsJson(value), WebPubSubDataType.Json, fireAndForget: true);
            }

            public Task SendEventAsync(string eventName, RawWebsocketData payload)
            {
                if (active && _closed)
                {
                    _context.OnClosed(_agent);
                    active = false;
                }

                return _socket.SendEventAsync(eventName, BinaryData.FromObjectAsJson(payload), WebPubSubDataType.Json, fireAndForget: true);
            }

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                await _socket.StartAsync(cancellationToken);
                active = true;
            }

            public async Task StopAsync()
            {
                await _socket.StopAsync();
                _connectionStoppedCts.Cancel();
            }
        }

        private async Task SendToAppServer(RawWebsocketData data)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, _appserverUrl)
            {
                Version = HttpVersion.Version20,
            };
            request.Content = new StringContent(data.Serilize(), Encoding.UTF8, "application/json");
            await HttpClient.SendAsync(request);
        }

        //
        private sealed class JoinGroup
        {
            public string type = "joinGroup";
            public string group;

            public JoinGroup(string group)
            {
                this.group = group;
            }
        }

        private sealed class SendToGroup
        {
            public string type = "sendToGroup";
            public string group;
            public string dataType = "text";
            public string data;

            public SendToGroup(string group, string data)
            {
                this.group = group;
                this.data = data;
            }
        }

        private sealed class UserEvent
        {
            public string type = "event";
            public string Event;
            public string dataType = "text";
            public string data;

            public UserEvent(string userEvent, string data)
            {
                this.Event = userEvent;
                this.data = data;
            }
        }

        private static string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(obj, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
        }
    }
    
}