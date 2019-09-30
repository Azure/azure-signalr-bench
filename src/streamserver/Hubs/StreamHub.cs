using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SignalRStreaming;

namespace SignalRChat.Hubs
{
    public class StreamHub : Hub
    {
        private HubBuffer _hubBuffer;

        public StreamHub(HubBuffer hubBuffer)
        {
            Console.WriteLine("Call streamhub");
            _hubBuffer = hubBuffer;
        }

        #region snippet1
        public ChannelReader<int> Counter(
            int count,
            int delay,
            CancellationToken cancellationToken)
        {
            var channel = Channel.CreateUnbounded<int>();

            // We don't want to await WriteItemsAsync, otherwise we'd end up waiting 
            // for all the items to be written before returning the channel back to
            // the client.
            _ = WriteItemsAsync(channel.Writer, count, delay, cancellationToken);

            return channel.Reader;
        }

        private async Task WriteItemsAsync(
            ChannelWriter<int> writer,
            int count,
            int delay,
            CancellationToken cancellationToken)
        {
            Exception localException = null;
            try
            {
                for (var i = 0; i < count; i++)
                {
                    await writer.WriteAsync(i, cancellationToken);

                    // Use the cancellationToken in other APIs that accept cancellation
                    // tokens so the cancellation can flow down to them.
                    await Task.Delay(delay, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                localException = ex;
            }

            writer.Complete(localException);
        }
        #endregion

        #region snippet2
        public async Task UploadStream(ChannelReader<string> stream)
        {
            Console.WriteLine("call uploadstream");
            while (await stream.WaitToReadAsync())
            {
                while (stream.TryRead(out var item))
                {
                    // do something with the stream item
                    Console.WriteLine(item);
                }
            }
        }
        #endregion

        #region snippet3
        public ChannelReader<string> PullStream(
            CancellationToken cancellationToken)
        {
            return _hubBuffer.BufferChannel[Context.ConnectionId];
        }

        private async Task WriteStreamItemsAsync(
            string id,
            ChannelWriter<string> writer,
            int delay,
            CancellationToken cancellationToken)
        {
            var input = _hubBuffer.BufferChannel[id];
            Console.WriteLine($"Process the incoming stream from {id}");
            Exception localException = null;
            try
            {
                while (await input.WaitToReadAsync())
                {
                    while (input.TryRead(out var item))
                    {
                        Console.WriteLine(item);
                        await writer.WriteAsync(item);
                        await Task.Delay(delay);
                    }
                }
            }
            catch (Exception ex)
            {
                localException = ex;
            }
            writer.Complete(localException);
        }

        public async Task SendStreamToClient(ChannelReader<string> stream, string targetConnectionId)
        {
            var channel = Channel.CreateUnbounded<string>();
            Console.WriteLine($"Send stream to {targetConnectionId}");
            Exception localException = null;
            bool informed = false;
            try
            {
                while (await stream.WaitToReadAsync())
                {
                    if (!informed)
                    {
                        _hubBuffer.BufferChannel[targetConnectionId] = channel;
                        await InformClientToPullStream(targetConnectionId);
                        informed = true;
                    }
                    while (stream.TryRead(out var item))
                    {
                        Console.WriteLine(item);
                        await channel.Writer.WriteAsync(item);
                        await Task.Delay(500);
                    }
                }
            }
            catch (Exception ex)
            {
                localException = ex;
            }
            channel.Writer.Complete(localException);
        }

        public async Task InformClientToPullStream(string targetConnectionId)
        {
            await Clients.Client(targetConnectionId).SendAsync("PleasePullStream");
            Console.WriteLine($"Inform {targetConnectionId} to receive streaming");
        }

        public override async Task OnConnectedAsync()
        {
            await Clients.Client(Context.ConnectionId).SendAsync("ConnectionId", Context.ConnectionId);
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            if (_hubBuffer.BufferChannel.ContainsKey(Context.ConnectionId))
            {
                _hubBuffer.BufferChannel.TryRemove(Context.ConnectionId, out _);
            }
            return Task.CompletedTask;
        }
        #endregion

        public void BroadcastMessage(string name, string message)
        {
            Clients.All.SendAsync("broadcastMessage", name, message);
        }

        public void Echo(string name, string message)
        {
            Clients.Client(Context.ConnectionId).SendAsync("echo", name, message);
        }
    }
}
