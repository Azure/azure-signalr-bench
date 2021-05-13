// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        private Latency _latency = new Latency();
        private int _recievedMessageCount;
        private int _expectedRecievedMessageCount;
        private int _sentMessageCount;
        private int _totalReconnectedCount;
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

        public int TotalReconnectedCount => Volatile.Read(ref _totalReconnectedCount);

        public int ReconnectingCount => _dict.Count(p => p.Value == ClientAgentStatus.Reconnecting);

        public int SentMessageCount => Volatile.Read(ref _sentMessageCount);

        public int RecievedMessageCount => Volatile.Read(ref _recievedMessageCount);

        public int ExpectedRecievedMessageCount => Volatile.Read(ref _expectedRecievedMessageCount);

        public int ConnectedAgentCount => _dict.Count(p => p.Value == ClientAgentStatus.Connected);

        public static long CoordinatedUtcNow()
        {
            return DateTime.UtcNow.Ticks + TimeBias;
        }

        public static void CoordinatorTime(long ticks)
        {
            TimeBias = (int) (ticks - DateTime.UtcNow.Ticks);
        }
        public async Task<string> GetConnectionIDAsync(int index)
        {
            return await _MessageClient.GetAsync(index.ToString());
        }

        public async Task SetConnectionIDAsync(int index, string connectionId)
        {
            await _MessageClient.SetAsync(index.ToString(), connectionId);
        }

        public void Measure(long ticks, string payload)
        {
            Interlocked.Increment(ref _recievedMessageCount);
            long latency = CoordinatedUtcNow() - ticks;
            if (latency < TimeSpan.TicksPerMillisecond * 50)
            {
                Interlocked.Increment(ref _latency.LessThan50ms);
            }
            else if (latency < TimeSpan.TicksPerMillisecond * 100)
            {
                Interlocked.Increment(ref _latency.LessThan100ms);
            }
            else if (latency < TimeSpan.TicksPerMillisecond * 200)
            {
                Interlocked.Increment(ref _latency.LessThan200ms);
            }
            else if (latency < TimeSpan.TicksPerMillisecond * 500)
            {
                Interlocked.Increment(ref _latency.LessThan500ms);
            }
            else if (latency < TimeSpan.TicksPerSecond)
            {
                Interlocked.Increment(ref _latency.LessThan1s);
            }
            else if (latency < TimeSpan.TicksPerSecond * 2)
            {
                Interlocked.Increment(ref _latency.LessThan2s);
            }
            else if (latency < TimeSpan.TicksPerSecond * 5)
            {
                Interlocked.Increment(ref _latency.LessThan5s);
            }
            else
            {
                Interlocked.Increment(ref _latency.MoreThan5s);
            }
        }

        public void IncreaseMessageSent(int expectedRecieverCount = 1)
        {
            Interlocked.Increment(ref _sentMessageCount);
            Interlocked.Add(ref _expectedRecievedMessageCount, expectedRecieverCount);
        }

        public async Task OnConnected(IClientAgent agent, bool hasGroups)
        {
            if (hasGroups)
            {
                _dict.AddOrUpdate(agent, ClientAgentStatus.Connected, (a, s) => ClientAgentStatus.JoiningGroups);
                await agent.JoinGroupAsync();
                _dict.AddOrUpdate(agent, ClientAgentStatus.Connected, (a, s) => ClientAgentStatus.Connected);
            }

            _dict.AddOrUpdate(agent, ClientAgentStatus.Connected, (a, s) =>
            {
                if (s == ClientAgentStatus.Reconnecting)
                    Interlocked.Increment(ref _totalReconnectedCount);
                return ClientAgentStatus.Connected;
            });
        }

        public Task OnReconnecting(IClientAgent agent)
        {
            _dict.AddOrUpdate(agent, ClientAgentStatus.Reconnecting, (a, s) => ClientAgentStatus.Reconnecting);
            return Task.CompletedTask;
        }

        public Task OnClosed(IClientAgent agent)
        {
            _dict.AddOrUpdate(agent, ClientAgentStatus.Reconnecting, (a, s) => ClientAgentStatus.Closed);
            return Task.CompletedTask;
        }

        public ReportClientStatusParameters ClientStatus() =>
            new ReportClientStatusParameters
            {
                TotalReconnectCount = TotalReconnectedCount,
                ConnectedCount = ConnectedAgentCount,
                ReconnectingCount = ReconnectingCount,
                MessageRecieved = RecievedMessageCount,
                ExpectedRecievedMessageCount = ExpectedRecievedMessageCount,
                MessageSent = SentMessageCount,
                Latency = GetLatency(),
            };

        public Dictionary<LatencyClass, int> GetLatency() =>
            new Dictionary<LatencyClass, int>
            {
                [LatencyClass.LessThan50ms] = Volatile.Read(ref _latency.LessThan50ms),
                [LatencyClass.LessThan100ms] = Volatile.Read(ref _latency.LessThan100ms),
                [LatencyClass.LessThan200ms] = Volatile.Read(ref _latency.LessThan200ms),
                [LatencyClass.LessThan500ms] = Volatile.Read(ref _latency.LessThan500ms),
                [LatencyClass.LessThan1s] = Volatile.Read(ref _latency.LessThan1s),
                [LatencyClass.LessThan2s] = Volatile.Read(ref _latency.LessThan2s),
                [LatencyClass.LessThan5s] = Volatile.Read(ref _latency.LessThan5s),
                [LatencyClass.MoreThan5s] = Volatile.Read(ref _latency.MoreThan5s),
            };

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
            //  _dict.Clear();
            _latency = new Latency();
            _recievedMessageCount = 0;
            _expectedRecievedMessageCount = 0;
            _sentMessageCount = 0;
            _totalReconnectedCount = 0;
        }
    }
}