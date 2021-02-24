using System;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using HubConnection = Microsoft.AspNet.SignalR.Client.HubConnection;

namespace Azure.SignalRBench.Client
{
    public class AspNetSignalRClientAgent : IClientAgent
    {
        //todo: ignore protocol and user name, implement them later
        public AspNetSignalRClientAgent(string url, string hub, Protocol protocol, string? userName, string[] groups,
            int globalIndex,
            ClientAgentContext context)
        {
            Context = context;
            Groups = groups;
            GlobalIndex = globalIndex;
            _protocol = protocol;
            Connection = new HubConnection(url);
            Console.WriteLine($"url:{url},hub:{hub}");
            HubProxy = Connection.CreateHubProxy(hub);
            HubProxy.On<long, string>(nameof(context.Measure), context.Measure);
            Connection.Reconnecting += () => context.OnReconnecting(this);
            Connection.Reconnected += async () =>
            {
                await Context.SetConnectionIDAsync(GlobalIndex, Connection.ConnectionId);
                await context.OnConnected(this, Groups.Length > 0);
            };
            Connection.Closed += async () =>
            {
                while (true)
                {
                    await Task.Delay(context.RetryPolicy.NextRetryDelay(null).Value.Milliseconds);
                    await StartAsync(default);
                }
            };
        }

        private HubConnection Connection { get; }

        private IHubProxy HubProxy { get; }
        public ClientAgentContext Context { get; }
        private string[] Groups { get; }
        public int GlobalIndex { get; }
        private Protocol _protocol;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Connection.State == ConnectionState.Disconnected)
                {
                    IClientTransport clientTransport = _protocol switch
                    {
                        Protocol.WebSocketsWithJson => new WebSocketTransport(),
                        Protocol.ServerSideEventsWithJson => new ServerSentEventsTransport(),
                        Protocol.LongPollingWithJson => new LongPollingTransport(),
                        _ => throw new Exception($"Unsupported protocal {_protocol} for aspnet")
                    };
                    await Connection.Start(clientTransport);
                    Console.WriteLine("connected");
                }

                await Context.SetConnectionIDAsync(GlobalIndex, Connection.ConnectionId);
                await Context.OnConnected(this, Groups.Length > 0);
            }
            catch (Exception ignore)
            {
                
            }
        }

        public Task StopAsync()
        {
            Connection.Stop();
            return Task.CompletedTask;
        }

        public Task EchoAsync(string payload) =>
            HubProxy.Invoke("Echo", DateTime.UtcNow.Ticks, payload);

        public async Task SendToClientAsync(int index, string payload)
        {
            var connectionID = await Context.GetConnectionIDAsync(index);
            await HubProxy.Invoke("SendToConnection", connectionID, DateTime.UtcNow.Ticks, payload);
        }

        public Task BroadcastAsync(string payload) =>
            HubProxy.Invoke("Broadcast", DateTime.UtcNow.Ticks, payload);

        public Task GroupBroadcastAsync(string group, string payload) =>
            HubProxy.Invoke("GroupBroadcast", group, DateTime.UtcNow.Ticks, payload);

        public Task JoinGroupAsync() => Task.WhenAll(Groups.Select(g => HubProxy.Invoke("JoinGroup", g)));

        private sealed class RetryPolicy : IRetryPolicy
        {
            public static readonly RetryPolicy Instance = new RetryPolicy();

            public TimeSpan? NextRetryDelay(RetryContext retryContext) =>
                TimeSpan.FromSeconds(1) + TimeSpan.FromSeconds(1) * StaticRandom.NextDouble();
        }
    }
}