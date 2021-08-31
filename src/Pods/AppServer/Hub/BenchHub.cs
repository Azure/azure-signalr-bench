// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Azure.SignalRBench.AppServer.Hub
{
    public class BenchHub : Microsoft.AspNetCore.SignalR.Hub
    {
        private ILogger<BenchHub> _logger;
        public BenchHub(ILogger<BenchHub> logger)
        {
            _logger = logger;
        }

        public override Task OnConnectedAsync()
        {
            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            return Task.CompletedTask;
        }

        public void Echo(long ticks, string payload)
        {
            Clients.Client(Context.ConnectionId).SendAsync("Measure", ticks, payload);
        }

        public void SendToConnection(string connectionId, long ticks, string payload)
        {
            Clients.Client(connectionId).SendAsync("Measure", ticks, payload);
        }

        public void Broadcast(long ticks, string payload)
        {
            Clients.All.SendAsync("Measure", ticks, payload);
        }

        public async Task JoinGroup(string group)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, group);
        }

        public async Task LeaveGroup(string group)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
        }

        public void GroupBroadcast(string group, long ticks, string payload)
        {
            Clients.Group(group).SendAsync("Measure", ticks, payload);
        }
    }
}