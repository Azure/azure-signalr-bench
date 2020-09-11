// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.SignalRBench.Common;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.SignalRBench.Client
{
    public class ClientAgent
    {
        private readonly HubConnection _connection;
        private readonly string[] _groups;
        public ClientAgentContext Context { get; }

        public ClientAgent(string url, SignalRProtocol protocol, string[] groups, string? userName, ClientAgentContext context)
        {
            _groups = groups;
            Context = context;
            _connection = new HubConnectionBuilder()
                .WithUrl(
                    url,
                    o =>
                    {
                        o.Transports = (HttpTransportType)((int)protocol & 0xF);
                        o.DefaultTransferFormat = (TransferFormat)((int)protocol >> 4);
                        if (userName != null)
                        {
                            o.Headers.Add("user", userName);
                        }
                    })
                .WithAutomaticReconnect(RetryPolicy.Instance)
                .Build();
            _connection.On<long, string>(nameof(context.Measure), context.Measure);
            _connection.Reconnecting += _ => context.OnReconnecting(this);
            _connection.Reconnected += _ => context.OnConnected(this, groups.Length > 0);
            _connection.Closed += _ => context.OnClosed(this);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _connection.StartAsync(cancellationToken);
            await Context.OnConnected(this, _groups.Length > 0);
        }

        public Task StopAsync() => _connection.StopAsync();

        public Task EchoAsync(string payload) =>
            _connection.SendAsync("Echo", DateTime.UtcNow.Ticks, payload);

        public Task BroadcastAsync(string payload) =>
            _connection.SendAsync("Broadcast", DateTime.UtcNow.Ticks, payload);

        public Task GroupBroadcastAsync(string group, string payload) =>
            _connection.SendAsync("GroupBroadcast", group, DateTime.UtcNow.Ticks, payload);

        public Task JoinGroupAsync() =>
            Task.WhenAll(_connection.SendAsync("JoinGroups", _groups));

        private sealed class RetryPolicy : IRetryPolicy
        {
            public static readonly RetryPolicy Instance = new RetryPolicy();

            public TimeSpan? NextRetryDelay(RetryContext retryContext) =>
                TimeSpan.FromSeconds(1) + TimeSpan.FromSeconds(1) * StaticRandom.NextDouble();
        }
    }
}
