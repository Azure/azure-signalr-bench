// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Azure.SignalRBench.Common;
using Microsoft.Extensions.Logging;

namespace Azure.SignalRBench.Client
{
    public sealed class ClientAgentBehaviorSettings
    {
        private readonly List<ClientBehaviorSetting> _settings = new List<ClientBehaviorSetting>();

        public ClientAgentBehaviorSettings(int listenCount)
        {
            ListenCount = listenCount;
        }

        public int ListenCount { get; }

        public void AddEcho(int start, int end, int size, TimeSpan interval)
        {
            _settings.Add(new EchoSetting(start, end, size, interval));
        }

        public void AddBroadcast(int start, int end, int size, int totalConnectionCount, TimeSpan interval)
        {
            _settings.Add(new BroadcastSetting(start, end, size, totalConnectionCount, interval));
        }

        public void AddGroup(int totalConnectionCount, int start, int end, int size, string groupFamily, int groupCount, int groupSize, TimeSpan interval)
        {
            _settings.Add(new GroupSetting( totalConnectionCount, start, end, size, groupFamily, groupCount, groupSize, interval));
        }

        public Action<ClientAgent, CancellationToken> GetClientAgentBehavior(int index, ILogger<ClientAgent> logger)
        {
            Action<ClientAgent, CancellationToken>? action = null;
            foreach (var setting in _settings)
            {
                if (setting.Match(index))
                {
                    action += (ca, ct) => _ = setting.RunAsync(ca, index, logger, ct);
                }
            }
            return action ?? EmptyAction;
        }

        private Action<ClientAgent, CancellationToken> EmptyAction { get; } = (ca, ct) => { };

        private abstract class ClientBehaviorSetting
        {
            protected ClientBehaviorSetting(int start, int end, int size)
            {
                Start = start;
                End = end;
                Payload = GenerateRandomData(size);
            }

            public int Start { get; }

            public int End { get; }

            public string Payload { get; }

            public bool Match(int index)
            {
                return index >= Start && index <End;
            }

            public abstract Task RunAsync(ClientAgent clientAgent, int clientId, ILogger<ClientAgent> logger, CancellationToken cancellationToken);

            private static string GenerateRandomData(int size)
            {
                var message = new byte[size * 3 / 4 + 1];
                StaticRandom.NextBytes(message);
                return Convert.ToBase64String(message).Substring(0, size);
            }
        }

        private sealed class EchoSetting : ClientBehaviorSetting
        {
            public EchoSetting(int start, int end, int size, TimeSpan interval)
                : base(start, end, size)
            {
                Interval = interval;
            }

            public TimeSpan Interval { get; }

            public async override Task RunAsync(ClientAgent clientAgent, int clientId, ILogger<ClientAgent> logger, CancellationToken cancellationToken)
            {
                await Task.Delay(Interval * StaticRandom.NextDouble());
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await clientAgent.EchoAsync(Payload);
                        clientAgent.Context.IncreaseMessageSent(1);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to send echo message: clientId={clientId}.", clientId);
                    }
                    await Task.Delay(Interval);
                }
            }
        }

        private sealed class BroadcastSetting : ClientBehaviorSetting
        {
            public BroadcastSetting(int start, int end, int size, int totalConnectionCount, TimeSpan interval)
                : base(start, end, size)
            {
                Interval = interval;
                TotalConnectionCount = totalConnectionCount;
            }

            public TimeSpan Interval { get; }

            public int TotalConnectionCount { get; }

            public async override Task RunAsync(ClientAgent clientAgent, int clientId, ILogger<ClientAgent> logger, CancellationToken cancellationToken)
            {
                await Task.Delay(Interval * StaticRandom.NextDouble());
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await clientAgent.BroadcastAsync(Payload);
                        clientAgent.Context.IncreaseMessageSent(TotalConnectionCount);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to send broadcast message: clientId={clientId}.", clientId);
                    }
                    await Task.Delay(Interval);
                }
            }
        }

        private sealed class GroupSetting : ClientBehaviorSetting
        {
            public GroupSetting(int totalConnectionCount, int start, int end, int size, string groupFamily, int groupCount, int groupSize, TimeSpan interval)
              : base(start, end, size)
            {
                TotalConnectionCount = totalConnectionCount;
                GroupFamily = groupFamily;
                GroupCount = groupCount;
                GroupSize = groupSize;
                Interval = interval;
            }

            public int TotalConnectionCount;
            public string GroupFamily { get; }

            public int GroupCount { get; }

            public int GroupSize { get; }

            public TimeSpan Interval { get; }

            public async override Task RunAsync(ClientAgent clientAgent, int clientId, ILogger<ClientAgent> logger, CancellationToken cancellationToken)
            {
                await Task.Delay(Interval * StaticRandom.NextDouble());
                while (!cancellationToken.IsCancellationRequested)
                {
                    int groupIndex = StaticRandom.Next(GroupCount);
                    var expectedMessageDelta = GroupSize;
                    if (groupIndex == GroupCount - 1)
                    {
                        var tmp = TotalConnectionCount % GroupSize;
                        expectedMessageDelta = tmp==0?GroupSize:tmp;
                    }
                    try
                    {
                        await clientAgent.GroupBroadcastAsync(GroupFamily + "_" + groupIndex, Payload);
                        clientAgent.Context.IncreaseMessageSent(expectedMessageDelta);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to send echo message: clientId={clientId}, groupFamily={groupFamily}, groupIndex={groupIndex}, groupSize={groupSize}.", clientId, GroupFamily, groupIndex, GroupSize);
                    }
                    await Task.Delay(Interval);
                }
            }
        }
    }
}
