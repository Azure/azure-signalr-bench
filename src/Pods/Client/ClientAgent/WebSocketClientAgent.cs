using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
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

        private static readonly HttpClient HttpClient = new HttpClient();


        public WebSocketClientAgent(string url, string appserverUrl, Protocol protocol, string[] groups,
            int globalIndex,
            ClientAgentContext context)
        {
            Context = context;
            _appserverUrl = "http://" + appserverUrl;
            Connection = new WebSocketHubConnection(url, this, context);
            Connection.On(context.Measure);
            Groups = groups;
            GlobalIndex = globalIndex;
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
            var echoEvent = new UserEvent(NameConverter.GenerateHubName(Context.TestId),
                JsonConvert.SerializeObject(data));
            return Connection.SendAsync(echoEvent.Serialize());
        }

        public Task GroupBroadcastAsync(string group, string payload)
        {
            var data = new RawWebsocketData()
            {
                Ticks = ClientAgentContext.CoordinatedUtcNow(),
                Payload = payload
            };
            var sendToGroup = new SendToGroup(group, JsonConvert.SerializeObject(data));
            return Connection.SendAsync(sendToGroup.Serilize());
        }

        public async Task JoinGroupAsync()
        {
            await Connection.SendAsync(new JoinGroup(Groups[0]).Serilize());
        }

        //This method should be sent directly to appserver to lower pressure on wps runtime 
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

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            await Connection.StartAsync(cancellationToken);
            await Context.OnConnected(this, Groups.Length > 0);
        }

        public async Task StopAsync()
        {
            await Connection.StopAsync();
        }


        private sealed class WebSocketHubConnection
        {
            private readonly ReliableWebsocketClient _socket;
            private readonly CancellationTokenSource _connectionStoppedCts = new CancellationTokenSource();
            private readonly WebSocketClientAgent _agent;
            private readonly SequenceId _sequenceId = new SequenceId();
            private ClientAgentContext _context;
            public Uri ResourceUri { get; }
            private CancellationToken ConnectionStoppedToken => _connectionStoppedCts.Token;

            public WebSocketHubConnection(string url, WebSocketClientAgent agent, ClientAgentContext context)
            {
                ResourceUri = new Uri(url);
                _socket = new ReliableWebsocketClient(ResourceUri);
                _agent = agent;
                _context = context;

                _socket.OnClose = () => _context.OnClosed(_agent);
            }

            public void On(Action<long, string> callback)
            {
                _socket.OnMessage = callback;
            }

            public volatile bool active = false;

            public Task SendAsync(string payload)
            {
                if (active && _socket.ConnectionState == ReliableWebsocketClient.State.Closed)
                {
                    _context.OnClosed(_agent);
                    active = false;
                }

                return _socket.SendAsync(Encoding.UTF8.GetBytes(payload), WebSocketMessageType.Text, true, default);
            }

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                await _socket.ConnectAsync(cancellationToken);
                active = true;
            }

            public async Task StopAsync()
            {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closed", default);
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

            public string Serilize()
            {
                return JsonConvert.SerializeObject(this);
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

            public string Serilize()
            {
                return JsonConvert.SerializeObject(this);
            }
        }

        private sealed class UserEvent
        {
            public string type = "event";
            public string Event = "echo";
            public string dataType = "text";
            public string data;

            public UserEvent(string userEvent, string data)
            {
                this.Event = userEvent;
                this.data = data;
            }

            public string Serialize()
            {
                return JsonConvert.SerializeObject(this, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
            }
        }

        private sealed class SequenceId
        {
            private readonly object _lock = new object();

            private ulong _sequenceId;

            private volatile bool _updated;

            public void UpdateSequenceId(ulong sequenceId)
            {
                lock (_lock)
                {
                    _sequenceId = sequenceId;
                    _updated = true;
                }
            }

            public bool TryGetSequenceId(out ulong sequenceId)
            {
                lock (_lock)
                {
                    if (_updated)
                    {
                        sequenceId = _sequenceId;
                        _updated = false;
                        return true;
                    }

                    sequenceId = 0;
                    return false;
                }
            }
        }

        private sealed class SequenceAck
        {
            public string type = "ack";
            public ulong sequenceId;

            public SequenceAck(ulong sequenceId)
            {
                this.sequenceId = sequenceId;
            }

            public string Serialize()
            {
                return JsonConvert.SerializeObject(this, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
            }
        }
    }
}