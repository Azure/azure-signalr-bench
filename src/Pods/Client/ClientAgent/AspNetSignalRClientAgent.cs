using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNetCore.SignalR.Client;
using HubConnection = Microsoft.AspNet.SignalR.Client.HubConnection;

namespace Azure.SignalRBench.Client.ClientAgent
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
                await Context.SetConnectionIdAsync(GlobalIndex, Connection.ConnectionId);
                await context.OnConnected(this, Groups.Length > 0);
            };
            Connection.Closed += async () =>
            {
                while (true)
                {
                    try
                    {
                        await Task.Delay(context.RetryPolicy.NextRetryDelay(null)?.Milliseconds ?? 1000);
                        await StartAsync(default);
                        return;
                    }
                    catch (Exception ignore)
                    {
                        // ignored
                    }
                }
            };
        }

        private HubConnection Connection { get; }

        private IHubProxy HubProxy { get; }
        public ClientAgentContext Context { get; }
        private string[] Groups { get; }
        public int GlobalIndex { get; }
        private readonly Protocol _protocol;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (Connection.State == ConnectionState.Disconnected)
            {
                IClientTransport clientTransport = _protocol switch
                {
                    Protocol.WebSocketsWithJson => new WebSocketTransport(),
                    Protocol.ServerSideEventsWithJson => new ServerSentEventsTransport(),
                    Protocol.LongPollingWithJson => new LongPollingTransport(),
                    _ => throw new Exception($"Unsupported protocol {_protocol} for aspnet")
                };
                var task= Connection.Start(clientTransport);
                var tcs = new TaskCompletionSource();
                if (!cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.Register(() => tcs.TrySetCanceled());
                    var runned=await Task.WhenAny(task, tcs.Task);
                    if (runned == task)
                    {
                        await Context.SetConnectionIdAsync(GlobalIndex, Connection.ConnectionId);
                        await Context.OnConnected(this, Groups.Length > 0);   
                    }
                }
            }
        }

        public Task StopAsync()
        {
            Connection.Stop();
            return Task.CompletedTask;
        }

        public Task EchoAsync(string payload) =>
            HubProxy.Invoke("Echo", ClientAgentContext.CoordinatedUtcNow(), payload);

        public async Task SendToClientAsync(int index, string payload)
        {
            var connectionId = await Context.GetConnectionIdAsync(index);
            await HubProxy.Invoke("SendToConnection", connectionId, ClientAgentContext.CoordinatedUtcNow(), payload);
        }

        public Task BroadcastAsync(string payload) =>
            HubProxy.Invoke("Broadcast", ClientAgentContext.CoordinatedUtcNow(), payload);

        public Task GroupBroadcastAsync(string group, string payload) =>
            HubProxy.Invoke("GroupBroadcast", group, ClientAgentContext.CoordinatedUtcNow(), payload);

        public Task JoinGroupAsync() => Task.WhenAll(Groups.Select(g => HubProxy.Invoke("JoinGroup", g)));

        private sealed class RetryPolicy : IRetryPolicy
        {
            public static readonly RetryPolicy Instance = new RetryPolicy();

            public TimeSpan? NextRetryDelay(RetryContext retryContext) =>
                TimeSpan.FromSeconds(1) + TimeSpan.FromSeconds(1) * StaticRandom.NextDouble();
        }
    }
}