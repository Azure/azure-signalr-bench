// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;

namespace Azure.SignalRBench.Client
{
    public class ClientAgentContext
    {
        private readonly ConcurrentDictionary<ClientAgent, ClientAgentStatus> _dict =
            new ConcurrentDictionary<ClientAgent, ClientAgentStatus>();

        private Latency _latency = new Latency();
        private int _recievedMessageCount;
        private int _expectedRecievedMessageCount;
        private int _sentMessageCount;
        private int _totalReconnectedCount;

        public int TotalReconnectedCount => Volatile.Read(ref _totalReconnectedCount);

        public int ReconnectingCount =>  _dict.Count(p => p.Value == ClientAgentStatus.Reconnecting);

        public int SentMessageCount => Volatile.Read(ref _sentMessageCount);

        public int RecievedMessageCount => Volatile.Read(ref _recievedMessageCount);

        public int ExpectedRecievedMessageCount => Volatile.Read(ref _expectedRecievedMessageCount);

        public int ConnectedAgentCount => _dict.Count(p => p.Value == ClientAgentStatus.Connected);

        public void Measure(long ticks, string payload)
        {
            Interlocked.Increment(ref _recievedMessageCount);
            long latency = DateTime.UtcNow.Ticks - ticks;
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

        public async Task OnConnected(ClientAgent agent, bool hasGroups)
        {
            if (hasGroups)
            {
                _dict.AddOrUpdate(agent, ClientAgentStatus.Connected, (a, s) => ClientAgentStatus.JoiningGroups);
                await agent.JoinGroupAsync();
            }

            _dict.AddOrUpdate(agent, ClientAgentStatus.Connected, (a, s) =>
            {
                if (s == ClientAgentStatus.Reconnecting)
                    Interlocked.Increment(ref _totalReconnectedCount);
                return ClientAgentStatus.Connected;

            });
        }

        public Task OnReconnecting(ClientAgent agent)
        {
            _dict.AddOrUpdate(agent, ClientAgentStatus.Reconnecting, (a, s) => ClientAgentStatus.Reconnecting);
            return Task.CompletedTask;
        }

        public Task OnClosed(ClientAgent agent)
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