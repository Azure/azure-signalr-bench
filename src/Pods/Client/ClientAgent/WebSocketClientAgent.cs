using System;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.WebPubSub;
using Azure.SignalRBench.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Azure.SignalRBench.Client
{
    class WebSocketClientAgent : IClientAgent
    {
        public ClientAgentContext Context { get; }
        public int GlobalIndex { get; }
        public string[] Groups { get; } = Array.Empty<string>();

        private WebSocketHubConnection Connection { get; }


        public WebSocketClientAgent(string url, Protocol protocol, string[] groups, int globalIndex,
            ClientAgentContext context)
        {
            Context = context;
            Connection = new WebSocketHubConnection(url,this,context);
            Connection.On(context.Measure);
            Groups = groups;
            GlobalIndex = globalIndex;
        }

        public Task BroadcastAsync(string payload)
        {
            var data = new RawWebsocketData()
            {
                Type = "broadcast",
                Ticks = ClientAgentContext.CoordinatedUtcNow(),
                Payload = payload
            };
            var broadcastEvent = new UserEvent(NameConverter.GenerateHubName(Context.TestId),
                JsonConvert.SerializeObject(data));
            return Connection.SendAsync(broadcastEvent.Serilize());
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
            return Connection.SendAsync(echoEvent.Serilize());
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

        public Task SendToClientAsync(int index, string payload)
        {
            var data = new RawWebsocketData()
            {
                Type = "p2p",
                Ticks = ClientAgentContext.CoordinatedUtcNow(),
                Payload = payload,
                Target = $"user{index}"
            };
            var p2PEvent = new UserEvent(NameConverter.GenerateHubName(Context.TestId),
                JsonConvert.SerializeObject(data));
            return Connection.SendAsync(p2PEvent.Serilize());
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
            private readonly ClientWebSocket _socket;
            private readonly CancellationTokenSource _connectionStoppedCts = new CancellationTokenSource();
            private readonly WebSocketClientAgent _agent;
            private ClientAgentContext _context;
            private Action<long, string>? _handler;
            public Uri ResourceUri { get; }
            private CancellationToken ConnectionStoppedToken => _connectionStoppedCts.Token;

            public WebSocketHubConnection(string url,WebSocketClientAgent agent,ClientAgentContext context)
            {
                _socket = new ClientWebSocket();
                _socket.Options.AddSubProtocol("json.webpubsub.azure.v1");
                ResourceUri = new Uri(url);
                _agent = agent;
                _context = context;
            }

            public void On(Action<long, string> callback)
            {
                _handler = callback;
            }

            public volatile bool active = false;
            public Task SendAsync(string payload)
            {
                if (active&&_socket.State == WebSocketState.Closed)
                {
                    _context.OnClosed(_agent);
                    active = false;
                }
                return _socket.SendAsync(Encoding.UTF8.GetBytes(payload), WebSocketMessageType.Text, true, default);
            }

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                await _socket.ConnectAsync(ResourceUri, cancellationToken);
                active = true;
                _ = ReceiveLoop();
            }

            public async Task StopAsync()
            {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closed", default);
                _connectionStoppedCts.Cancel();
            }

            private async Task ReceiveLoop()
            {
                while (_socket.State == WebSocketState.Open)
                {
                    var ms = new MemoryStream();
                    Memory<byte> buffer = new byte[1 << 10];
                    // receive loop
                    while (true)
                    {
                        var receiveResult = await _socket.ReceiveAsync(buffer, default);
                        // Need to check again for NetCoreApp2.2 because a close can happen between a 0-byte read and the actual read
                        if (receiveResult.MessageType == WebSocketMessageType.Close)
                        {
                            try
                            {
                                Console.WriteLine($"The connection closed");

                                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, default);
                            }
                            catch (Exception e)
                            {
                                // It is possible that the remote is already closed
                                Console.WriteLine($"The connection close:{e}");
                            }

                            break;
                        }

                        await ms.WriteAsync(buffer.Slice(0, receiveResult.Count));
                        if (receiveResult.EndOfMessage)
                        {
                            var str = Encoding.UTF8.GetString(ms.ToArray());
                            
                            var response = JsonConvert.DeserializeObject<MessageResponse>(str);
                            try
                            {
                                var data = JsonConvert.DeserializeObject<RawWebsocketData>(response.data);
                                _handler?.Invoke(data.Ticks, data.Payload);
                            }
                            catch
                            {
                                
                            }

                            ms.SetLength(0);
                        }
                    }
                }
            }
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

            public string Serilize()
            {
                return JsonConvert.SerializeObject(this, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
            }
        }

        private sealed class MessageResponse
        {
            public string type;
            public string from;
            public string data;
        }
    }
}