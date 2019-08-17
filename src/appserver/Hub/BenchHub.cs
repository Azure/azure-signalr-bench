// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Azure.SignalR.PerfTest.AppServer
{
    public class BenchHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await Clients.Client(Context.ConnectionId).SendAsync("OnConnected", Context.ConnectionId);
        }

        public void Echo(IDictionary<string, object> data)
        {
            Clients.Client(Context.ConnectionId).SendAsync("RecordLatency", data);
        }

        public void Broadcast(IDictionary<string, object> data)
        {
            Clients.All.SendAsync("RecordLatency", data);
        }

        public void SendToClient(IDictionary<string, object> data)
        {
            var targetId = (string)data["information.ConnectionId"];
            Clients.Client(targetId).SendAsync("RecordLatency", data);
        }

        public void ConnectionId()
        {
            Clients.Client(Context.ConnectionId).SendAsync("ConnectionId", Context.ConnectionId);
        }

        public string GetConnectionId()
        {
            return Context.ConnectionId;
        }

        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            await Clients.Client(Context.ConnectionId).SendAsync("JoinGroup");
        }

        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            await Clients.Client(Context.ConnectionId).SendAsync("LeaveGroup");
        }

        public void SendToGroup(IDictionary<string, object> data)
        {
            var groupName = (string)data["information.GroupName"];
            Clients.Group(groupName).SendAsync("RecordLatency", data);
        }
    }
}