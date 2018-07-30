using System;
using Microsoft.AspNetCore.SignalR;

namespace ChatSample
{
    public class Chat : Hub
    {
        public void BroadcastMessage(string name, string message)
        {
            Clients.All.SendAsync("broadcastMessage", name, message);
        }

        public void Echo(string name, string message)
        {
            Clients.Client(Context.ConnectionId).SendAsync("echo", name, message);
        }

        public void SendToGroup(string groupName, string message)
        {
            Clients.Group(groupName).SendAsync("SendToGroup", Context.ConnectionId, message);
        }

        public void JoinGroup(string groupName, string client)
        {
            Groups.AddToGroupAsync(Context.ConnectionId, groupName);
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

        public void LeaveGroup(string groupName, string client)
        {
            Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            if (string.Equals(client, "perf", StringComparison.Ordinal))
            {
                Clients.Client(Context.ConnectionId).SendAsync("LeaveGroup", Context.ConnectionId, $"{Context.ConnectionId} left {groupName}");
            }
            else
            {
                Clients.Group(groupName).SendAsync("LeaveGroup", Context.ConnectionId, $"{Context.ConnectionId} left {groupName}");
            }
        }
    }
}
