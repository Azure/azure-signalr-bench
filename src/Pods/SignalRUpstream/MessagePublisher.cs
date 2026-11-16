using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace SignalRUpstream
{
    public class MessagePublisher
    {
        private const string Target = "Measure";
        private readonly EndpointHubContexts _hubContexts;

        public MessagePublisher(EndpointHubContexts hubContexts)
        {
            _hubContexts = hubContexts;
        }

        public async Task ManageUserGroupAsync(string command, string userId, string groupName)
        {
            var hubContext = _hubContexts.PickRandom();
            switch (command)
            {
                case "add":
                    await hubContext.UserGroups.AddToGroupAsync(userId, groupName);
                    break;
                case "remove":
                    await hubContext.UserGroups.RemoveFromGroupAsync(userId, groupName);
                    break;
                default:
                    Console.WriteLine($"Can't recognize command {command}");
                    return;
            }
        }

        public async Task SendMessagesAsync(string command, string receiver, long ticks, string payload)
        {
            var hubContext = _hubContexts.PickRandom();
            switch (command)
            {
                case "broadcast":
                    await hubContext.Clients.All.SendAsync(Target, ticks, payload);
                    break;
                case "user":
                    var userId = receiver;
                    await hubContext.Clients.User(userId).SendAsync(Target, ticks, payload);
                    break;
                // case "users":
                //     var userIds = receiver.Split(',');
                //     return _hubContext.Clients.Users(userIds).SendAsync(Target, message);
                case "group":
                    var groupName = receiver;
                    await hubContext.Clients.Group(groupName).SendAsync(Target, ticks, payload);
                    break;
                // case "groups":
                //     var groupNames = receiver.Split(',');
                //     return _hubContext.Clients.Groups(groupNames).SendAsync(Target, message);
                default:
                    Console.WriteLine($"Can't recognize command {command}");
                    return;
            }
        }
    }
}