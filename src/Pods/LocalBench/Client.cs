using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR.Client;

namespace LocalBench;

public static class Client
{
    public static async Task RunAsync(int clientNum, CancellationToken token)
    {
        var clientAgentContext = new ClientAgentContext();
        var agents = new List<ClientAgent>();
        Console.WriteLine("Start client connections...\n");
        for (var i = 0; i < clientNum; i++)
        {
            try
            {
                agents.Add(new ClientAgent(clientAgentContext));
                _ = agents[i].StartAsync(token);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }
        }

        await Task.Delay(3000, token);

        var message = new byte[2000];
        new Random().NextBytes(message);
        var payload = Convert.ToBase64String(message)[..2000];

        while (!token.IsCancellationRequested)
        {
            Console.WriteLine("Start to send message...");
            agents.ForEach(a => a.EchoAsync(payload));
            await Task.Delay(3000);
            clientAgentContext.Report();
            clientAgentContext.Reset();
        }
    }


    public class ClientAgent
    {
        private ClientAgentContext Context;

        public ClientAgent(ClientAgentContext context)
        {
            Context = context;
            var builder =
                new HubConnectionBuilder()
                    .WithUrl(
                        Server.Url + "/signalrbench"
                    );

            builder.AddJsonProtocol();
            Connection = builder.Build();
            Connection.On<long, string>(nameof(context.Measure), context.Measure);
            Connection.Closed += _ => context.OnClosed(this);
        }

        public HubConnection Connection { get; }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (Connection.State == HubConnectionState.Disconnected)
            {
                await Connection.StartAsync(cancellationToken);
            }

            await Context.OnConnected(this);
        }

        public Task StopAsync() => Connection.StopAsync();

        public async Task EchoAsync(string payload)
        {
            Context.IncreaseMessageSent();
            await Connection.SendAsync("Echo", DateTime.UtcNow.Ticks, payload);
        }
    }

    public class ClientAgentContext
    {
        private readonly ConcurrentDictionary<ClientAgent, ClientAgentStatus> _dict = new();

        private volatile int _recievedMessageCount;
        private volatile int _sentMessageCount;
        private volatile int _latencySum;

        public int ConnectedAgentCount => _dict.Count(p => p.Value == ClientAgentStatus.Connected);

        public void Measure(long ticks, string payload)
        {
            Interlocked.Increment(ref _recievedMessageCount);
            long latency = DateTime.UtcNow.Ticks - ticks;
            _latencySum += (int) latency;
        }

        public void IncreaseMessageSent()
        {
            Interlocked.Increment(ref _sentMessageCount);
        }

        public Task OnConnected(ClientAgent agent)
        {
            _dict.AddOrUpdate(agent, ClientAgentStatus.Connected, (a, s) => ClientAgentStatus.Connected);
            return Task.CompletedTask;
        }

        public Task OnClosed(ClientAgent agent)
        {
            _dict.AddOrUpdate(agent, ClientAgentStatus.Closed, (a, s) => ClientAgentStatus.Closed);
            return Task.CompletedTask;
        }

        public void Report()
        {
            Console.WriteLine($"Message sent: {_sentMessageCount}, Message received: {_recievedMessageCount}");
            Console.WriteLine(_recievedMessageCount == 0
                ? "No message received"
                : $"Average Latency: {_latencySum / _recievedMessageCount / TimeSpan.TicksPerMillisecond} ms\n");
        }

        public void Reset()
        {
            _sentMessageCount = 0;
            _recievedMessageCount = 0;
            _latencySum = 0;
        }

        private enum ClientAgentStatus
        {
            Connected,
            Closed,
        }
    }
}