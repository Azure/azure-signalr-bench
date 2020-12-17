using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Logging;
using SignalRUpstream.Entities;

namespace SignalRUpstream
{
    public class MessagePublisher
    {
        private const string Target = "Measure";
        private string _hubName;
        private readonly string _connectionString;
        private readonly ServiceTransportType _serviceTransportType;
        private IServiceHubContext _hubContext;

        public MessagePublisher(string connectionString,string hubName, ServiceTransportType serviceTransportType)
        {
            _connectionString = connectionString;
            _hubName = hubName;
            _serviceTransportType = serviceTransportType;
            InitAsync().Wait();
        }

        public async Task InitAsync()
        {
            var serviceManager = new ServiceManagerBuilder().WithOptions(option =>
            {
                option.ConnectionString = _connectionString;
                option.ServiceTransportType = _serviceTransportType;
            }).Build();

            _hubContext = await serviceManager.CreateHubContextAsync(_hubName, new LoggerFactory());
        }

        public async Task ManageUserGroupAsync(string command, string userId, string groupName)
        {
            switch (command)
            {
                case "add":
                    await _hubContext.UserGroups.AddToGroupAsync(userId, groupName);
                    break;
                case "remove":
                    await _hubContext.UserGroups.RemoveFromGroupAsync(userId, groupName);
                    break;
                default:
                    Console.WriteLine($"Can't recognize command {command}");
                    return;
            }
        }

        public async Task SendMessagesAsync(string command, string receiver,long ticks, string payload)
        {
            switch (command)
            {
                case "broadcast":
                    await _hubContext.Clients.All.SendAsync(Target, ticks,payload);
                    break;
                case "user":
                    var userId = receiver;
                    await _hubContext.Clients.User(userId).SendAsync(Target, ticks,payload);
                    break;
                // case "users":
                //     var userIds = receiver.Split(',');
                //     return _hubContext.Clients.Users(userIds).SendAsync(Target, message);
                case "group":
                    var groupName = receiver;
                    await _hubContext.Clients.Group(groupName).SendAsync(Target, ticks,payload);
                    break;
                // case "groups":
                //     var groupNames = receiver.Split(',');
                //     return _hubContext.Clients.Groups(groupNames).SendAsync(Target, message);
                default:
                    Console.WriteLine($"Can't recognize command {command}");
                    return;
            }
        }

        public Task DisposeAsync() => _hubContext?.DisposeAsync();
    }
}