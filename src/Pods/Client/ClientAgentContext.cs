// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.SignalRBench.Client.ClientAgent;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Messages;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Azure.SignalRBench.Client
{
    public class ClientAgentContext
    {
        private readonly ConcurrentDictionary<IClientAgent, ClientAgentStatus> _dict =
            new ConcurrentDictionary<IClientAgent, ClientAgentStatus>();

        private static volatile int TimeBias;
        private Counters _counters = new Counters();
        public string TestId { get; set; }
        private MessageClient _MessageClient;
        private readonly ILogger<ClientAgentContext> _logger;

        public ClientAgentContext(MessageClient messageClient, ILogger<ClientAgentContext> logger)
        {
            _MessageClient = messageClient;
            TestId = _MessageClient.TestId;
            _logger = logger;
        }

        public IRetryPolicy RetryPolicy { get; set; }

        public int TotalReconnectedCount => Volatile.Read(ref _counters.TotalReconnectedCount);

        public int ReconnectingCount => _dict.Count(p => p.Value == ClientAgentStatus.Reconnecting);

        public int SentMessageCount => Volatile.Read(ref _counters.SentMessageCount);

        public int RecievedMessageCount => Volatile.Read(ref _counters.RecievedMessageCount);

        public int ExpectedRecievedMessageCount => Volatile.Read(ref _counters.ExpectedRecievedMessageCount);

        public int ConnectedAgentCount => _dict.Count(p => p.Value == ClientAgentStatus.Connected);

        public static long CoordinatedUtcNow()
        {
            return DateTime.UtcNow.Ticks + TimeBias;
        }

        public static void CoordinatorTime(long ticks)
        {
            TimeBias = (int) (ticks - DateTime.UtcNow.Ticks);
        }
        public async Task<string> GetConnectionIdAsync(int index)
        {
            return await _MessageClient.GetAsync(index.ToString());
        }

        public async Task SetConnectionIdAsync(int index, string connectionId)
        {
            await _MessageClient.SetAsync(index.ToString(), connectionId);
        }

        public void Measure(long ticks, string payload)
        {
            var counters = Volatile.Read(ref _counters);
            Interlocked.Increment(ref counters.RecievedMessageCount);
            long latency = CoordinatedUtcNow() - ticks;
            ClientMetrics.ObserveClientLatency((double)latency/TimeSpan.TicksPerSecond);
            if (latency < TimeSpan.TicksPerMillisecond * 50)
            {
                Interlocked.Increment(ref counters.Latency.LessThan50ms);
            }
            else if (latency < TimeSpan.TicksPerMillisecond * 100)
            {
                Interlocked.Increment(ref counters.Latency.LessThan100ms);
            }
            else if (latency < TimeSpan.TicksPerMillisecond * 200)
            {
                Interlocked.Increment(ref counters.Latency.LessThan200ms);
            }
            else if (latency < TimeSpan.TicksPerMillisecond * 500)
            {
                Interlocked.Increment(ref counters.Latency.LessThan500ms);
            }
            else if (latency < TimeSpan.TicksPerSecond)
            {
                Interlocked.Increment(ref counters.Latency.LessThan1s);
            }
            else if (latency < TimeSpan.TicksPerSecond * 2)
            {
                Interlocked.Increment(ref counters.Latency.LessThan2s);
            }
            else if (latency < TimeSpan.TicksPerSecond * 5)
            {
                Interlocked.Increment(ref counters.Latency.LessThan5s);
            }
            else
            {
                Interlocked.Increment(ref counters.Latency.MoreThan5s);
            }
        }
        
        public void IncreaseReceivedClientAckCount()
        {
            var counters = Volatile.Read(ref _counters);
            Interlocked.Increment(ref counters.ReceivedClientAckCount);
        }
        
        public void IncreaseReceivedServerAckCount()
        {
            var counters = Volatile.Read(ref _counters);
            Interlocked.Increment(ref counters.ReceivedServerAckCount);
        }

        public void IncreaseMessageSent(int expectedRecieverCount = 1)
        {
            var counters = Volatile.Read(ref _counters);
            Interlocked.Increment(ref counters.SentMessageCount);
            Interlocked.Add(ref counters.ExpectedRecievedMessageCount, expectedRecieverCount);
        }

        public async Task OnConnected(IClientAgent agent, bool hasGroups)
        {
            var reconnected = _dict.TryGetValue(agent, out var previous) &&
                previous == ClientAgentStatus.Reconnecting;
            if (hasGroups)
            {
                _dict.AddOrUpdate(agent, ClientAgentStatus.JoiningGroups, (a, s) => ClientAgentStatus.JoiningGroups);
                await agent.JoinGroupAsync();
            }
            _dict.AddOrUpdate(agent, ClientAgentStatus.Connected, (a, s) => ClientAgentStatus.Connected);
            if (reconnected)
            {
                var counters = Volatile.Read(ref _counters);
                Interlocked.Increment(ref counters.TotalReconnectedCount);
                ClientMetrics.IncClientReconnectCount();
            }
        }

        public Task OnReconnecting(IClientAgent agent)
        {
            _dict.AddOrUpdate(agent, ClientAgentStatus.Reconnecting, (a, s) => ClientAgentStatus.Reconnecting);
            return Task.CompletedTask;
        }

        public Task OnClosed(IClientAgent agent)
        {
            _dict.AddOrUpdate(agent, ClientAgentStatus.Closed, (a, s) => ClientAgentStatus.Closed);
            return Task.CompletedTask;
        }

        public ReportClientStatusParameters ClientStatus()
        {
            var counters = Volatile.Read(ref _counters);
            return new ReportClientStatusParameters
            {
                TotalReconnectCount = Volatile.Read(ref counters.TotalReconnectedCount),
                ConnectedCount = ConnectedAgentCount,
                ReconnectingCount = ReconnectingCount,
                MessageRecieved = Volatile.Read(ref counters.RecievedMessageCount),
                ExpectedRecievedMessageCount = Volatile.Read(ref counters.ExpectedRecievedMessageCount),
                MessageSent = Volatile.Read(ref counters.SentMessageCount),
                ClientReceivedServerAckCount = Volatile.Read(ref counters.ReceivedServerAckCount),
                ServerReceivedClientAckCount = Volatile.Read(ref counters.ReceivedClientAckCount),
                Latency = GetLatency(counters),
            };
        }

        public Dictionary<LatencyClass, int> GetLatency() => GetLatency(Volatile.Read(ref _counters));

        private static Dictionary<LatencyClass, int> GetLatency(Counters counters) =>
            new Dictionary<LatencyClass, int>
            {
                [LatencyClass.LessThan50ms] = Volatile.Read(ref counters.Latency.LessThan50ms),
                [LatencyClass.LessThan100ms] = Volatile.Read(ref counters.Latency.LessThan100ms),
                [LatencyClass.LessThan200ms] = Volatile.Read(ref counters.Latency.LessThan200ms),
                [LatencyClass.LessThan500ms] = Volatile.Read(ref counters.Latency.LessThan500ms),
                [LatencyClass.LessThan1s] = Volatile.Read(ref counters.Latency.LessThan1s),
                [LatencyClass.LessThan2s] = Volatile.Read(ref counters.Latency.LessThan2s),
                [LatencyClass.LessThan5s] = Volatile.Read(ref counters.Latency.LessThan5s),
                [LatencyClass.MoreThan5s] = Volatile.Read(ref counters.Latency.MoreThan5s),
            };

        private sealed class Counters
        {
            public readonly Latency Latency = new Latency();
            public int ReceivedServerAckCount;
            public int RecievedMessageCount;
            public int ExpectedRecievedMessageCount;
            public int ReceivedClientAckCount;
            public int SentMessageCount;
            public int TotalReconnectedCount;
        }

        private enum ClientAgentStatus
        {
            JoiningGroups,
            Connected,
            Reconnecting,
            Closed,
        }

        private sealed class Latency
        {
            public int LessThan50ms;
            public int LessThan100ms;
            public int LessThan200ms;
            public int LessThan500ms;
            public int LessThan1s;
            public int LessThan2s;
            public int LessThan5s;
            public int MoreThan5s;
        }

        public void Reset()
        {
            Interlocked.Exchange(ref _counters, new Counters());
        }
    }
}