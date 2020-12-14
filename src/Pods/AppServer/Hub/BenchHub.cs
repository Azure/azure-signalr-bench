// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Azure.SignalRBench.AppServer
{
    public class BenchHub : Hub
    {
        private ILogger<BenchHub> _logger;
        public BenchHub(ILogger<BenchHub> logger)
        {
            _logger = logger;
        }
     
        public override Task OnConnectedAsync()
        {
        //    _logger.LogInformation("connected: id:{id}", Context.ConnectionId);
            return Task.CompletedTask;
        }
        
        public override Task OnDisconnectedAsync(Exception exception)
        {
          //  _logger.LogInformation("disconnected: id:{id}", Context.ConnectionId);
            return Task.CompletedTask;
        }

        public void Echo(long ticks, string payload)
        {
         //   _logger.LogInformation("echo , payload:{payload}", payload);
            Clients.Client(Context.ConnectionId).SendAsync("Measure", ticks, payload);
        }

        public void Broadcast(long ticks, string payload)
        {
           // _logger.LogInformation("broadcast , payload:{payload}", payload);
            Clients.All.SendAsync("Measure", ticks, payload);
        }

        public async Task JoinGroups(string group)
        {
                await Groups.AddToGroupAsync(Context.ConnectionId, group);
        }

        public async Task LeaveGroups(string group)
        {
            //   _logger.LogInformation("leave group {group}", group);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
        }

        public void GroupBroadcast(string group, long ticks, string payload)
        {
            Clients.Group(group).SendAsync("Measure", ticks, payload);
        }
    }
}