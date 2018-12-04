// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace ChatRoom
{
    public class SignalRBench : Hub
    {
        public void Send(string name, string message)
        {
            // Call the broadcastMessage method to update clients.
            Clients.All.broadcastMessage(name, message);
        }

        public Task TestEcho(string name, string message)
        {
            Clients.Client(Context.ConnectionId).testEchoBack(name, message);
            return Task.CompletedTask;
        }

        public Task Echo(IDictionary<string, object> data)
        {
            Clients.Client(Context.ConnectionId).RecordLatency(data);
            return Task.CompletedTask;
        }

        public void Broadcast(IDictionary<string, object> data)
        {
            Clients.All.RecordLatency(data);
        }

        public void SendToClient(IDictionary<string, object> data)
        {
            var targetId = (string)data["information.ConnectionId"];
            Clients.Client(targetId).RecordLatency(data);
        }

        public string GetConnectionId()
        {
            return Context.ConnectionId;
        }

        public async Task JoinGroup(string groupName)
        {
            await Groups.Add(Context.ConnectionId, groupName);
            Clients.Client(Context.ConnectionId).JoinGroup();
        }

        public async Task LeaveGroup(string groupName)
        {
            await Groups.Remove(Context.ConnectionId, groupName);
            Clients.Client(Context.ConnectionId).LeaveGroup();
        }

        public void SendToGroup(IDictionary<string, object> data)
        {
            var groupName = (string)data["information.GroupName"];
            Clients.Group(groupName).RecordLatency(data);
        }
    }
}