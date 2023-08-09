using System.Threading;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Microsoft.Extensions.Logging;
using SocketIOClient;

namespace Azure.SignalRBench.Client.ClientAgent
{
    public class SioClientAgent : IClientAgent
    {
        public ClientAgentContext Context { get; }
        public int GlobalIndex { get; }
        public SocketIO Client { get; }
        public string[] Groups { get; }

        public bool ClientExpectServerAck { get; }

        public bool ServerExpectClientAck { get; }

        public ILogger<SioClientAgent> Logger;

        public SioClientAgent(string url, ClientAgentContext context, string[] groups, int globalIndex,
            bool clientExpectServerAck,
            bool serverExpectClientAck, ILoggerFactory loggerFactory)
        {
            Context = context;
            GlobalIndex = globalIndex;
            Groups = groups;
            ClientExpectServerAck = clientExpectServerAck;
            ServerExpectClientAck = serverExpectClientAck;
            Logger = loggerFactory.CreateLogger<SioClientAgent>();

            Client = new SocketIO(url, new SocketIOOptions()
            {
                Path = $"/clients/socketio/hubs/{PerfConstants.Name.HubName}"
            });

            Client.OnDisconnected += async (sender, args) => { await Context.OnClosed(this); };
            Client.OnReconnectAttempt += (sender, args) => { Context.OnReconnecting(this); };
            Client.OnReconnected += async (sender, args) =>
            {
                await Context.SetConnectionIdAsync(GlobalIndex, Client.Id);
                await Context.OnConnected(this, Groups.Length > 0);
            };

            Client.On("Measure", async response => { Measure(response); });
            Client.On("MeasureWithAck", async response =>
            {
                await response.CallbackAsync("ack");
                Measure(response);
            });

            // Not necessary
            // Client.On("ServerNotifyAckReceived", async response =>
            // {
            //     RecordAckDelay(response);
            // });

            void Measure(SocketIOResponse response)
            {
                var startTime = response.GetValue<long>(0);
                var payload = response.GetValue<string>(1);
                context.Measure(startTime, payload);
            }

            void RecordAckDelay(SocketIOResponse response)
            {
                // var startTime = response.GetValue<long>(0);
                context.IncreaseReceivedClientAckCount();
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Client.ConnectAsync();
            if (!Client.Connected)
            {
                await Client.ConnectAsync();
            }
            await Context.SetConnectionIdAsync(GlobalIndex, Client.Id);
            await Context.OnConnected(this, Groups.Length > 0);
        }

        public Task StopAsync()
        {
            return Client.DisconnectAsync();
        }

        public async Task EchoAsync(string payload)
        {
            var eventName = "Echo" + (ServerExpectClientAck ? "WithAck" : string.Empty);
            if (ClientExpectServerAck)
            {
                await Client.EmitAsync(eventName, ServerAckClient, ClientAgentContext.CoordinatedUtcNow(), payload);
            }
            else
            {
                await Client.EmitAsync(eventName, ClientAgentContext.CoordinatedUtcNow(), payload);
            }
        }

        public async Task SendToClientAsync(int index, string payload)
        {
            var eventName = "SendToSocket" + (ServerExpectClientAck ? "WithAck" : string.Empty);
            var connectionId = await Context.GetConnectionIdAsync(index);
            if (ClientExpectServerAck)
            {
                await Client.EmitAsync(eventName, ServerAckClient, connectionId,
                    ClientAgentContext.CoordinatedUtcNow(), payload);
            }
            else
            {
                await Client.EmitAsync(eventName, connectionId, ClientAgentContext.CoordinatedUtcNow(), payload);
            }
        }

        public async Task BroadcastAsync(string payload)
        {
            var eventName = "Broadcast" + (ServerExpectClientAck ? "WithAck" : string.Empty);
            if (ClientExpectServerAck)
            {
                await Client.EmitAsync(eventName, ServerAckClient, ClientAgentContext.CoordinatedUtcNow(), payload);
            }
            else
            {
                await Client.EmitAsync(eventName, ClientAgentContext.CoordinatedUtcNow(), payload);
            }
        }

        public async Task GroupBroadcastAsync(string group, string payload)
        {
            var eventName = "SendToGroup" + (ServerExpectClientAck ? "WithAck" : string.Empty);
            if (ClientExpectServerAck)
            {
                await Client.EmitAsync(eventName, ServerAckClient, group, ClientAgentContext.CoordinatedUtcNow(),
                    payload);
            }
            else
            {
                await Client.EmitAsync(eventName, group, ClientAgentContext.CoordinatedUtcNow(), payload);
            }
        }

        public async Task JoinGroupAsync()
        {
            await Client.EmitAsync("JoinGroup", Groups[0]);
        }

        private void ServerAckClient(SocketIOResponse response)
        {
            Context.IncreaseReceivedServerAckCount();
        }
    }
}