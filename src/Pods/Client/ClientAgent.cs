// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace Azure.SignalRBench.Client
{
    public class ClientAgent
    {
        private readonly HubConnection _connection;

        public ClientAgentContext Context { get; }

        public string[] Groups { get; } = Array.Empty<string>();

        
        //todo: implement group and userName
        public ClientAgent(string url, SignalRProtocol protocol, string? userName, string[] groups,
            ClientAgentContext context)
        {
            Context = context;
            Groups = groups;
            _connection = new HubConnectionBuilder()
                .WithUrl(
                    url + "signalrbench",
                    o =>
                    {
                        o.Transports = (HttpTransportType) ((int) protocol & 0xF);
                        o.DefaultTransferFormat = (TransferFormat) ((int) protocol >> 4);
                        if (userName != null)
                        {
                           // o.Headers.Add("user", userName);
                        }
                    }
                )
                .WithAutomaticReconnect(RetryPolicy.Instance)
                .Build();
            _connection.On<long, string>(nameof(context.Measure), context.Measure);
            _connection.Reconnecting += _ => context.OnReconnecting(this);
            _connection.Reconnected += _ => context.OnConnected(this, Groups.Length > 0);
            _connection.Closed += _ => context.OnClosed(this);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if(_connection.State == HubConnectionState.Disconnected)
                  await _connection.StartAsync(cancellationToken);
            await Context.OnConnected(this, Groups.Length > 0);
        }

        public Task StopAsync() => _connection.StopAsync();

        public Task EchoAsync(string payload) =>
            _connection.SendAsync("Echo", DateTime.UtcNow.Ticks, payload);

        public Task BroadcastAsync(string payload) =>
            _connection.SendAsync("Broadcast", DateTime.UtcNow.Ticks, payload);

        public Task GroupBroadcastAsync(string group, string payload) =>
            _connection.SendAsync("GroupBroadcast", group, DateTime.UtcNow.Ticks, payload);

        public Task JoinGroupAsync() => Task.WhenAll(Groups.Select(g => _connection.InvokeAsync("JoinGroups", g)));

        private sealed class RetryPolicy : IRetryPolicy
        {
            public static readonly RetryPolicy Instance = new RetryPolicy();

            public TimeSpan? NextRetryDelay(RetryContext retryContext) =>
                TimeSpan.FromSeconds(1) + TimeSpan.FromSeconds(1) * StaticRandom.NextDouble();
        }
    }
}