using System;
using System.Linq;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Logging;
using SignalRUpstream.Entities;

namespace SignalRUpstream
{
    public class MessagePublisher
    {
        private const string Target = "Measure";
        private readonly string _hubName;
        private readonly string _connectionString;
        private readonly ServiceTransportType _serviceTransportType;
        private IServiceHubContext[] _hubContext;

        public MessagePublisher(string connectionString,string testId, ServiceTransportType serviceTransportType)
        {
            _connectionString = connectionString;
            _hubName = NameConverter.GenerateHubName(testId);
            _serviceTransportType = serviceTransportType;
            InitAsync().Wait();
        }

        public async Task InitAsync()
        {
            var connectionStrings = _connectionString.Split(" ");
            _hubContext = new IServiceHubContext[connectionStrings.Length];
            for (var i = 0; i < connectionStrings.Length; i++)
            {
                var serviceManager = new ServiceManagerBuilder().WithOptions(option =>
                {
                    option.ConnectionString = connectionStrings[i];
                    option.ServiceTransportType = _serviceTransportType;
                }).Build();
                _hubContext[i] = await serviceManager.CreateHubContextAsync(_hubName, new LoggerFactory());
            }
        }

        public async Task ManageUserGroupAsync(string command, string userId, string groupName)
        {
            var index = StaticRandom.Next(_hubContext.Length);
            switch (command)
            {
                case "add":
                    await _hubContext[index].UserGroups.AddToGroupAsync(userId, groupName);
                    break;
                case "remove":
                    await _hubContext[index].UserGroups.RemoveFromGroupAsync(userId, groupName);
                    break;
                default:
                    Console.WriteLine($"Can't recognize command {command}");
                    return;
            }
        }

        public async Task SendMessagesAsync(string command, string receiver, long ticks, string payload)
        {
            var index = StaticRandom.Next(_hubContext.Length);
            switch (command)
            {
                case "broadcast":
                    await _hubContext[index].Clients.All.SendAsync(Target, ticks, payload);
                    break;
                case "user":
                    var userId = receiver;
                    await _hubContext[index].Clients.User(userId).SendAsync(Target, ticks, payload);
                    break;
                // case "users":
                //     var userIds = receiver.Split(',');
                //     return _hubContext.Clients.Users(userIds).SendAsync(Target, message);
                case "group":
                    var groupName = receiver;
                    await _hubContext[index].Clients.Group(groupName).SendAsync(Target, ticks, payload);
                    break;
                // case "groups":
                //     var groupNames = receiver.Split(',');
                //     return _hubContext.Clients.Groups(groupNames).SendAsync(Target, message);
                default:
                    Console.WriteLine($"Can't recognize command {command}");
                    return;
            }
        }

        public Task DisposeAsync() => Task.WhenAll(_hubContext.Select(hub => hub.DisposeAsync()));
    }
}