// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;

namespace AspNetAppServer
{
    public class Signalrbench : Hub
    {
        public void Echo(long ticks, string payload)
        {
            Clients.Caller.Measure(ticks, payload);
        }

        public void SendToConnection(string connectionId, long ticks, string payload)
        {
            Clients.Client(connectionId).Measure( ticks, payload);
        }

        public void Broadcast(long ticks, string payload)
        {
            Clients.All.Measure(ticks, payload);
        }

        public async Task JoinGroup(string group)
        {
            await Groups.Add(Context.ConnectionId, group);
        }

        public async Task LeaveGroup(string group)
        {
            await Groups.Remove(Context.ConnectionId, group);
        }

        public void GroupBroadcast(string group, long ticks, string payload)
        {
            Clients.Group(group).Measure(ticks, payload);
        }
    }
}
