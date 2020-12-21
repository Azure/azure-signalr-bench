using System;
using System.CodeDom.Compiler;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Newtonsoft.Json;

namespace Azure.SignalRBench.Client
{
    class WebSocketClientAgent : IClientAgent
    {
        public ClientAgentContext Context { get; }
        public int GlobalIndex { get; }
        public string[] Groups { get; } = Array.Empty<string>();

        private WebSocketHubConnection Connection { get; }
        public WebSocketClientAgent(string url, Protocol protocol, string[] groups, int globalIndex, ClientAgentContext context)
        {
            Connection = new WebSocketHubConnection(url);
            Connection.On(context.Measure);
            Context = context;
            Groups = groups;
            GlobalIndex = globalIndex;
        }

        public Task SendToClientAsync(int index, string payload)
        {
            throw new NotImplementedException();
        }

        public Task BroadcastAsync(string payload) => throw new NotImplementedException();

        public Task EchoAsync(string payload)
        {
            var data = new Data()
            {
                Ticks = DateTime.Now.Ticks,
                Payload = payload
            };
            return Connection.SendAsync(JsonConvert.SerializeObject(data));
        }

        public Task GroupBroadcastAsync(string group, string payload) => throw new NotImplementedException();

        public Task JoinGroupAsync() => throw new NotImplementedException();

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Connection.StartAsync(cancellationToken);
            await Context.OnConnected(this, false);
        }

        public async Task StopAsync()
        {
            await Connection.StopAsync();
        }

        private sealed class Data
        {
            public string Payload { get; set; }
            public long Ticks { get; set; }
        }

        private sealed class WebSocketHubConnection
        {
            private readonly ClientWebSocket _socket;
            private CancellationTokenSource _connectionStoppedCts = new CancellationTokenSource();

            private Action<long, string>? _handler;
            public Uri Endpoint { get; }
            private CancellationToken ConnectionStoppedToken => _connectionStoppedCts.Token;
            public WebSocketHubConnection(string url)
            {
                _socket = new ClientWebSocket();
                Endpoint = new Uri(url);
            }

            public void On(Action<long, string> callback)
            {
                _handler = callback;
            }

            public Task SendAsync(string payload)
            {
                return _socket.SendAsync(Encoding.UTF8.GetBytes(payload), WebSocketMessageType.Text, true, default);
            }

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                await _socket.ConnectAsync(Endpoint, cancellationToken);
                _ = ReceiveLoop();
            }

            public async Task StopAsync()
            {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closed", default);
                _connectionStoppedCts.Cancel();
            }

            private async Task ReceiveLoop()
            {
                var buffer = new byte[1 << 10];
                while (_socket.State == WebSocketState.Open)
                {
                    try
                    {
                        var response = await _socket.ReceiveAsync(buffer, ConnectionStoppedToken);
                        var dataStr = Encoding.UTF8.GetString(buffer, 0, response.Count);
                        var data = JsonConvert.DeserializeObject<Data>(dataStr);
                        _handler?.Invoke(data.Ticks, data.Payload);
                    }
                    catch (OperationCanceledException)
                    {
                        continue;
                    }
                }
            }
        }
    }
}
