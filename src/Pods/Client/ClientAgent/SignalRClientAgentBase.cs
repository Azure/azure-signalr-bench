using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace Azure.SignalRBench.Client
{
    public abstract class SignalRClientAgentBase : IClientAgent
    {
        public SignalRClientAgentBase(string urlWithHub, Protocol protocol, string? userName, string[] groups, int globalIndex,
            ClientAgentContext context)
        {
            Context = context;
            Groups = groups;
            GlobalIndex = globalIndex;
            Connection = HubConnectionBuilderExtensions.WithAutomaticReconnect(new HubConnectionBuilder()
                    .WithUrl(
                        urlWithHub,
                        o =>
                        {
                            o.Transports = (HttpTransportType)((int)protocol & 0xF);
                            o.DefaultTransferFormat = (TransferFormat)((int)protocol >> 4);
                            if (userName != null)
                            {
                                o.Headers.Add("user", userName);
                            }
                        }
                    ), context.RetryPolicy)
                .Build();
            Connection.On<long, string>(nameof(context.Measure), context.Measure);
            Connection.Reconnecting += _ => context.OnReconnecting(this);
            Connection.Reconnected += async _ =>
            {
                await Context.SetConnectionIDAsync(GlobalIndex, Connection.ConnectionId);
                await context.OnConnected(this, Groups.Length > 0);
            };
            Connection.Closed += _ => context.OnClosed(this);
        }

        public HubConnection Connection { get; }
        public ClientAgentContext Context { get; }
        public string[] Groups { get; } = Array.Empty<string>();
        public int GlobalIndex { get; }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (Connection.State == HubConnectionState.Disconnected)
            {
                await Connection.StartAsync(cancellationToken);
            }
            await Context.SetConnectionIDAsync(GlobalIndex, Connection.ConnectionId);
            await Context.OnConnected(this, Groups.Length > 0);
        }

        public Task StopAsync() => Connection.StopAsync();
        public abstract Task EchoAsync(string payload);
        public abstract Task SendToClientAsync(int index, string payload);
        public abstract Task BroadcastAsync(string payload);
        public abstract Task GroupBroadcastAsync(string group, string payload);
        public abstract Task JoinGroupAsync();
        
    }
}