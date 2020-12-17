// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Messages;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace Azure.SignalRBench.Client
{
    public class ClientAgent
    {
        internal HubConnection Connection { get; }

        public ClientAgentContext Context { get; }

        public string[] Groups { get; } = Array.Empty<string>();

        public int GlobalIndex { get; }

        public ClientAgent(string url, SignalRProtocol protocol, string? userName, string[] groups, int globalIndex,
            ClientAgentContext context)
        {
            Context = context;
            Groups = groups;
            GlobalIndex = globalIndex;
            Connection = new HubConnectionBuilder()
                .WithUrl(
                    url + "signalrbench",
                    o =>
                    {
                        o.Transports = (HttpTransportType) ((int) protocol & 0xF);
                        o.DefaultTransferFormat = (TransferFormat) ((int) protocol >> 4);
                        if (userName != null)
                        {
                            o.Headers.Add("user", userName);
                        }
                    }
                )
                .WithAutomaticReconnect(RetryPolicy.Instance)
                .Build();
            Connection.On<long, string>(nameof(context.Measure), context.Measure);
            Connection.Reconnecting += _ => context.OnReconnecting(this);
            Connection.Reconnected += _ => context.OnConnected(this, Groups.Length > 0);
            Connection.Closed += _ => context.OnClosed(this);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (Connection.State == HubConnectionState.Disconnected)
            {
                await Connection.StartAsync(cancellationToken);
            }

            await Context.OnConnected(this, Groups.Length > 0);
        }

        public Task StopAsync() => Connection.StopAsync();

        public Task EchoAsync(string payload) =>
            Connection.SendAsync("Echo", DateTime.UtcNow.Ticks, payload);

        public async Task SendToClientAsync(int index, string payload)
        {
            var connectionID = await Context.GetConnectionIDAsync(index);
            await Connection.SendAsync("SendToConnection", connectionID, DateTime.UtcNow.Ticks, payload);
        }

        public Task BroadcastAsync(string payload) =>
            Connection.SendAsync("Broadcast", DateTime.UtcNow.Ticks, payload);

        public Task GroupBroadcastAsync(string group, string payload) =>
            Connection.SendAsync("GroupBroadcast", group, DateTime.UtcNow.Ticks, payload);

        public Task JoinGroupAsync() => Task.WhenAll(Groups.Select(g => Connection.InvokeAsync("JoinGroups", g)));

        private sealed class RetryPolicy : IRetryPolicy
        {
            public static readonly RetryPolicy Instance = new RetryPolicy();

            public TimeSpan? NextRetryDelay(RetryContext retryContext) =>
                TimeSpan.FromSeconds(1) + TimeSpan.FromSeconds(1) * StaticRandom.NextDouble();
        }
    }
}