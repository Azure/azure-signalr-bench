// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Interlocked = System.Threading.Interlocked;

namespace Microsoft.Azure.SignalR.PerfTest.AppServer
{
    public class BenchHub : Hub
    {
        private static int _totalReceivedEcho = 0;
        private static int _totalReceivedBroadcast = 0;
        private static int _totalReceivedGroup = 0;

        public void ConnectionId()
        {
            Clients.Client(Context.ConnectionId).SendAsync("connectionId", Context.ConnectionId);
        }

        public void SendToClient(string targetId, string time, byte[] messageBlob)
        {
            Clients.Client(targetId).SendAsync("sendToClient", 0, time, Context.ConnectionId, targetId, messageBlob);

        }
        public void Echo(string uid, string time, byte[] messageBlob)
        {
            Interlocked.Increment(ref _totalReceivedEcho);
            Clients.Client(Context.ConnectionId).SendAsync("echo", _totalReceivedEcho, time, Context.ConnectionId, null, messageBlob);
        }

        public void Broadcast(string uid, string time, byte[] messageBlob)
        {
            Interlocked.Increment(ref _totalReceivedBroadcast);
            Clients.All.SendAsync("broadcast", _totalReceivedBroadcast, time, Context.ConnectionId, null, messageBlob);
        }

        public void BroadcastMessage(string name, string message)
        {
            Clients.All.SendAsync("broadcastMessage", name, message);
        }

        public void SendToGroup(string groupName, string message)
        {
            Clients.Group(groupName).SendAsync("SendToGroup", Context.ConnectionId, message);
        }

        public void SendGroup(string groupName, string time, byte[] messageBlob)
        {
            Interlocked.Increment(ref _totalReceivedGroup);
            Console.WriteLine($"{groupName}");
            Clients.Group(groupName).SendAsync("SendGroup", 0, time, groupName, null, messageBlob);

        }

        public async Task JoinGroup(string groupName, string client)
        {
            // Console.WriteLine($"join group: {groupName}");
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            // Console.WriteLine($"join group end: {groupName}");
            if (string.Equals(client, "perf", StringComparison.Ordinal))
            {
                // for perf test
                Clients.Client(Context.ConnectionId).SendAsync("JoinGroup", Context.ConnectionId, $"{Context.ConnectionId} joined {groupName}");
            }
            else
            {
                Clients.Group(groupName).SendAsync("JoinGroup", Context.ConnectionId, $"{Context.ConnectionId} joined {groupName}");
            }

        }

        public async Task LeaveGroup(string groupName, string client)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            if (string.Equals(client, "perf", StringComparison.Ordinal))
            {
                Clients.Client(Context.ConnectionId).SendAsync("LeaveGroup", Context.ConnectionId, $"{Context.ConnectionId} left {groupName}");
            }
            else
            {
                Clients.Group(groupName).SendAsync("LeaveGroup", Context.ConnectionId, $"{Context.ConnectionId} left {groupName}");
            }

        }

        public void Count(string name)
        {
            var count = 0;
            if (name == "echo") count = _totalReceivedEcho;
            if (name == "broadcast") count = _totalReceivedBroadcast;
            if (name == "group") count = _totalReceivedGroup;
            Clients.Client(Context.ConnectionId).SendAsync("count", count);
        }
    }
}