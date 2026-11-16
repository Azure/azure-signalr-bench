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
    class WebSocketClientAgent : IClientAgent
    {
        public ClientAgentContext Context { get; }
        public int GlobalIndex { get; }
        public string[] Groups { get; }

        private WebSocketHubConnection Connection { get; }

        private readonly string _appserverUrl;
        private readonly ILogger<WebSocketClientAgent> _logger;

        private static readonly HttpClient HttpClient = new HttpClient();

        public WebSocketClientAgent(string url, string appserverUrl, Protocol protocol, string[] groups,
            int globalIndex,
            ClientAgentContext context,
            ILoggerFactory loggerFactory)
        {
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
        public virtual Task BroadcastAsync(string payload, CancellationToken cancellationToken = default)
        {
            var data = new RawWebsocketData()
            {
                Type = "broadcast",
                Ticks = ClientAgentContext.CoordinatedUtcNow(),
                Payload = payload
            };
            return SendToAppServer(data, cancellationToken);
        }

        public virtual Task EchoAsync(string payload, CancellationToken cancellationToken = default)
        {
            var data = new RawWebsocketData()
            {
                Type = "echo",
                Ticks = ClientAgentContext.CoordinatedUtcNow(),
                Payload = payload
            };
            return Connection.SendEventAsync(NameConverter.GenerateHubName(Context.TestId), data, cancellationToken);
        }

        public virtual Task GroupBroadcastAsync(string group, string payload, CancellationToken cancellationToken = default)
        {
            var data = new RawWebsocketData()
            {
                Ticks = ClientAgentContext.CoordinatedUtcNow(),
                Payload = payload
            };
            return Connection.SendToGroup(group, data, cancellationToken);
        }
        
        //This method is sent directly to appserver to lower pressure on wps runtime 
        public virtual Task SendToClientAsync(int index, string payload, CancellationToken cancellationToken = default)
        {
            var data = new RawWebsocketData()
            {
                Type = "p2p",
                Ticks = ClientAgentContext.CoordinatedUtcNow(),
                Payload = payload,
                Target = $"user{index}"
            };
            return SendToAppServer(data, cancellationToken);
        }


        private sealed class WebSocketHubConnection
        {
            private readonly WebPubSubClient _socket;
            private readonly WebSocketClientAgent _agent;
            private readonly ClientAgentContext _context;

            private volatile bool _started;
            private volatile bool _stopping;
            private volatile bool _closed;
            private volatile bool _reconnecting;
            public Uri ResourceUri { get; }

            public WebSocketHubConnection(string url, WebSocketClientAgent agent, Protocol protocol,
                ClientAgentContext context, ILogger logger)
            {
                ResourceUri = new Uri(url);
                var options = new WebPubSubClientOptions
                {
                    // ClientAgentContext.OnConnected owns group joins (with SDK retry) and status transitions.
                    AutoRejoinGroups = false,
                    Protocol = protocol switch
                    {
                        Protocol.RawWebSocketReliableJson => new WebPubSubJsonReliableProtocol(),
                        Protocol.RawWebSocketJson => new WebPubSubJsonProtocol(),
                        Protocol.RawWebSocketReliableProtobuf => new ProtobufSubprotocol(),
                        _ => throw new Exception($"Unsupported protocol {protocol}"),
                    },
                };
                _socket = new WebPubSubClient(ResourceUri, options);
                _agent = agent;
                _context = context;

                _socket.Disconnected += _ =>
                {
                    _closed = true;
                    if (_stopping)
                    {
                        _reconnecting = false;
                        return _context.OnClosed(_agent);
                    }
                    if (!_started)
                    {
                        return Task.CompletedTask;
                    }
                    _reconnecting = true;
                    return _context.OnReconnecting(_agent);
                };
                _socket.Connected += _ =>
                {
                    _closed = false;
                    if (!_reconnecting)
                    {
                        return Task.CompletedTask;
                    }
                    _reconnecting = false;
                    return _context.OnConnected(_agent, _agent.Groups.Length > 0);
                };
                _socket.Stopped += _ =>
                {
                    _closed = true;
                    _reconnecting = false;
                    return _context.OnClosed(_agent);
                };
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

            public Task JoinGroup(string group) => _socket.JoinGroupAsync(group);

            public Task SendToGroup(string group, RawWebsocketData value, CancellationToken cancellationToken) =>
                _socket.SendToGroupAsync(group, BinaryData.FromObjectAsJson(value), WebPubSubDataType.Json,
                    fireAndForget: true, cancellationToken: cancellationToken);

            public Task SendEventAsync(string eventName, RawWebsocketData payload, CancellationToken cancellationToken) =>
                _socket.SendEventAsync(eventName, BinaryData.FromObjectAsJson(payload), WebPubSubDataType.Json,
                    fireAndForget: true, cancellationToken: cancellationToken);

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                _stopping = false;
                _reconnecting = false;
                _closed = false;
                _started = false;
                await _socket.StartAsync(cancellationToken);
                await _context.OnConnected(_agent, _agent.Groups.Length > 0);
                _started = true;
                if (_closed && !_stopping)
                {
                    _reconnecting = true;
                    await _context.OnReconnecting(_agent);
                }
            }

            public async Task StopAsync()
            {
                _stopping = true;
                _reconnecting = false;
                await _socket.StopAsync();
                await _context.OnClosed(_agent);
            }
        }

        protected async Task SendToAppServer(RawWebsocketData data, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, _appserverUrl)
            {
                Version = HttpVersion.Version20,
            };
            var binaryData = BinaryData.FromObjectAsJson(data);
            request.Content = new StringContent(binaryData.ToString(), Encoding.UTF8, "application/json");
            using var response = await HttpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
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