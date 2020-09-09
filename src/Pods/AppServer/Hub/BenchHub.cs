// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Azure.SignalRBench.AppServer
{
    public class BenchHub : Hub
    {
        public void Echo(long ticks, string payload)
        {
            Clients.Client(Context.ConnectionId).SendAsync("Measure", ticks, payload);
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
