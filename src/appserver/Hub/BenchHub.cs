// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using Plugin.Microsoft.Azure.SignalR.Benchmark;

namespace Microsoft.Azure.SignalR.PerfTest.AppServer
{
    public class BenchHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await Clients.Client(Context.ConnectionId).SendAsync("OnConnected", Context.ConnectionId);
        }

        public void Echo(BenchMessage data)
        {
            Clients.Client(Context.ConnectionId).SendAsync("RecordLatency", data);
        }

        public ChannelReader<BenchMessage> StreamingEcho(ChannelReader<BenchMessage> stream, int delay)
        {
            var channel = Channel.CreateUnbounded<BenchMessage>();
            async Task WriteChannelStream()
            {
                Exception localException = null;
                try
                {
                    while (await stream.WaitToReadAsync())
                    {
                        while (stream.TryRead(out var item))
                        {
                            await channel.Writer.WriteAsync(item);
                            if (delay > 0)
                            {
                                await Task.Delay(delay);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    localException = ex;
                }
                channel.Writer.Complete(localException);
            }
            _ = WriteChannelStream();
            return channel.Reader;
        }

        public void Broadcast(BenchMessage data)
        {
            Clients.All.SendAsync("RecordLatency", data);
        }

        public void SendToClient(BenchMessage data)
        {
            Clients.Client(data.Target).SendAsync("RecordLatency", data);
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

        public void SendToGroup(BenchMessage data)
        {
            Clients.Group(data.Target).SendAsync("RecordLatency", data);
        }
    }
}
