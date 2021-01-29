// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Azure.SignalRBench.Client.Exceptions;
using Azure.SignalRBench.Common;
using Microsoft.Extensions.Logging;

namespace Azure.SignalRBench.Client
{
    public class ScenarioState : IScenarioState
    {
        private readonly ILoggerFactory _loggerFactory;
        private ScenarioBaseState _state;
        private ScenarioBaseState _save = null;
        private int totalConnected = 0;
        private int[] indexMap = new int[0];
        private IClientAgentFactory _clientAgentFactory;
        public ClientAgentContainer? ClientAgentContainer { get; set; }

        public ScenarioState(ILoggerFactory loggerFactory, IClientAgentFactory clientAgentFactory)
        {
            _loggerFactory = loggerFactory;
            _state = new RoundInitState(this);
            _clientAgentFactory = clientAgentFactory;
        }

        public void SetClientRange(SetClientRangeParameters setClientRangeParameters) =>
            _state.SetClientRange(setClientRangeParameters);

        public void SetSenario(SetScenarioParameters setScenarioParameters) =>
            _state.SetSenario(setScenarioParameters);

        public Task StartClientConnections(MessageClientHolder messageClientHolder,
            StartClientConnectionsParameters startClientConnectionsParameters) =>
            _state.StartClientConnections(messageClientHolder, startClientConnectionsParameters);

        public void StartSenario(StartScenarioParameters startScenarioParameters) =>
            _state.StartSenario(startScenarioParameters);

        public void StopSenario(StopScenarioParameters stopScenario) =>
            _state.StopSenario(stopScenario);

        public Task StopClientConnections(StopClientConnectionsParameters stopClientConnectionsParameters) =>
            _state.StopClientConnections(stopClientConnectionsParameters);

        private abstract class ScenarioBaseState
        {
            public ScenarioState ScenarioState { get; }

            public ScenarioBaseState(ScenarioState scenarioState)
            {
                ScenarioState = scenarioState;
            }

            public void SetState(ScenarioBaseState state)
            {
                ScenarioState._state = state;
            }

            public void Save()
            {
                ScenarioState._save = this;
            }

            public ILogger<T> GetLogger<T>() => ScenarioState._loggerFactory.CreateLogger<T>();

            public virtual void SetClientRange(SetClientRangeParameters setClientRangeParameters) =>
                throw new InvalidScenarioStateException();

            public virtual Task StartClientConnections(MessageClientHolder messageClientHolder,
                StartClientConnectionsParameters startClientConnectionsParameters) =>
                throw new InvalidScenarioStateException();

            public virtual void SetSenario(SetScenarioParameters setScenarioParameters) =>
                throw new InvalidScenarioStateException();

            public virtual void StartSenario(StartScenarioParameters startScenarioParameters) =>
                throw new InvalidScenarioStateException();

            public virtual void StopSenario(StopScenarioParameters stopScenario) =>
                throw new InvalidScenarioStateException();

            public virtual Task
                StopClientConnections(StopClientConnectionsParameters stopClientConnectionsParameters) =>
                throw new InvalidScenarioStateException();
        }

        private sealed class RoundInitState : ScenarioBaseState
        {
            private ILogger<RoundInitState> _logger;
            public RoundInitState(ScenarioState scenarioState)
                : base(scenarioState)
            {
                _logger = GetLogger<RoundInitState>();
                Save();
            }

            public override void SetClientRange(SetClientRangeParameters setClientRangeParameters)
            {
                var indexMapDelta = GenerateIndexMap(setClientRangeParameters.TotalCountDelta, setClientRangeParameters.StartIdTruncated, setClientRangeParameters.LocalCountDelta);
                var tmp = new int[ScenarioState.indexMap.Length + indexMapDelta.Length];
                for (int i = 0; i < ScenarioState.indexMap.Length; i++)
                {
                    tmp[i] = ScenarioState.indexMap[i];
                }
                int postion = ScenarioState.indexMap.Length;
                for (int i = 0; i < indexMapDelta.Length; i++)
                {
                    tmp[postion + i] = indexMapDelta[i] + ScenarioState.totalConnected;
                }
                ScenarioState.indexMap = tmp;
                int oldTotalConnected = ScenarioState.totalConnected;
                ScenarioState.totalConnected += setClientRangeParameters.TotalCountDelta;
                _logger.LogInformation($"Indexmap generated. Total connected:{ScenarioState.totalConnected}, localConnected:{ScenarioState.indexMap.Length}");
                SetState(new ClientRangeReadyState(ScenarioState, oldTotalConnected + setClientRangeParameters.StartIdTruncated, ScenarioState.indexMap.Length));
            }

            public override async Task StopClientConnections(
                StopClientConnectionsParameters stopClientConnectionsParameters)
            {
                if (ScenarioState.ClientAgentContainer != null)
                    await ScenarioState.ClientAgentContainer.StopAsync(stopClientConnectionsParameters.Rate);
                SetState(new RoundInitState(ScenarioState));
            }

            private static int[] GenerateIndexMap(int total, int startIndex, int count)
            {
                var rand = new Random(total);
                return (from id in Enumerable.Range(0, total)
                        orderby rand.Next()
                        select id).Skip(startIndex).Take(count).ToArray();
            }
        }

        private sealed class ClientRangeReadyState : ScenarioBaseState
        {
            public int StartId { get; }

            public int LocalCount { get; }

            private ILogger<ClientRangeReadyState> _logger;

            public ClientRangeReadyState(ScenarioState scenarioState, int startId, int localCount)
                : base(scenarioState)
            {
                StartId = startId;
                LocalCount = localCount;
                _logger = GetLogger<ClientRangeReadyState>();
            }

            public override async Task StartClientConnections(MessageClientHolder messageClientHolder,
                StartClientConnectionsParameters p)
            {
                try
                {
                    int continueIndex = 0;
                    _logger.LogInformation($"StartId:{StartId},LocalCount:{LocalCount}");
                    if (ScenarioState.ClientAgentContainer == null)
                    {
                        ScenarioState.ClientAgentContainer = new ClientAgentContainer(
                            messageClientHolder,
                            p.Protocol,
                            p.IsAnonymous,
                            p.Url,
                            p.ClientLifetime, ScenarioState._clientAgentFactory,
                            ScenarioState._loggerFactory);
                    }
                    continueIndex = ScenarioState.ClientAgentContainer.ExpandConnections(StartId, LocalCount, ScenarioState.indexMap, GetGroupsFunc(ScenarioState.totalConnected, ScenarioState.indexMap, p.GroupDefinitions));
                    _logger.LogInformation($"continueIndex:{continueIndex}");

                    await ScenarioState.ClientAgentContainer.StartAsync(continueIndex, p.Rate, default);
                    _logger.LogInformation("Connections started.");
                    SetState(new ClientsReadyState(ScenarioState, p.GroupDefinitions,
                        ScenarioState.ClientAgentContainer));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            private Func<int, string[]> GetGroupsFunc(int total, int[] indexMap, GroupDefinition[] groupDefinitions)
            {
                if (groupDefinitions.Length == 0)
                {
                    return _ => Array.Empty<string>();
                }

                return index =>
                {
                    var mapId = indexMap[index];
                    var result = new List<string>();
                    var current = 0;
                    foreach (var gd in groupDefinitions)
                    {
                        for (int gi = 0; gi < gd.GroupCount; gi++)
                        {
                            if (IsInGroup(total, mapId, current, gd.GroupSize))
                            {
                                result.Add(gd.GroupFamily + "_" + gi.ToString());
                            }
                            current += gd.GroupSize;
                            current %= total;
                        }
                    }
                    _logger.LogInformation($"mapId:{mapId} is in group {result[0]}");
                    return result.ToArray();
                };
            }

            private static bool IsInGroup(int total, int index, int start, int size)
            {
                int end = start + size;
                if (index >= start && index < end)
                {
                    return true;
                }

                // if (end >= total)
                // {
                //     return index + total < end;
                // }

                return false;
            }
        }

        private sealed class ClientsReadyState : ScenarioBaseState
        {

            public GroupDefinition[] GroupDefinitions { get; }

            public ClientAgentContainer ClientAgentContainer { get; }

            public ClientsReadyState(ScenarioState scenarioState,
                GroupDefinition[] groupDefinitions, ClientAgentContainer clientAgentContainer)
                : base(scenarioState)
            {
                GroupDefinitions = groupDefinitions;
                ClientAgentContainer = clientAgentContainer;
            }

            public override void SetSenario(SetScenarioParameters setScenarioParameters)
            {
                ParseParameters(setScenarioParameters, out var listen, out var echoList, out var p2pList, out var broadcastList,
                    out var groupBroadcastList);
                var counts = echoList.Select(x => x.Count)
                    .Concat(p2pList.Select(x => x.Count))
                    .Concat(broadcastList.Select(x => x.Count))
                    .Concat(groupBroadcastList.Select(x => x.Count));
                var max = counts.Max();
                if (listen > ScenarioState.totalConnected - max)
                {
                    listen = ScenarioState.totalConnected - max;
                }
                var sum = counts.Sum();
                if (listen < ScenarioState.totalConnected - sum)
                {
                    listen = ScenarioState.totalConnected - sum;
                }
                SetState(
                    new ScenarioReadyState(
                        ScenarioState,
                        ClientAgentContainer,
                        CreateSettings(listen, echoList, p2pList, broadcastList, groupBroadcastList)));
            }


            private static void ParseParameters(
                SetScenarioParameters setScenarioParameters,
                out int listen,
                out List<ClientBehaviorDetailDefinition> echoList,
                out List<ClientBehaviorDetailDefinition> p2pList,
                out List<ClientBehaviorDetailDefinition> broadcastList,
                out List<GroupClientBehaviorDetailDefinition> groupBroadcastList)
            {
                listen = 0;
                echoList = new List<ClientBehaviorDetailDefinition>();
                p2pList = new List<ClientBehaviorDetailDefinition>();
                broadcastList = new List<ClientBehaviorDetailDefinition>();
                groupBroadcastList = new List<GroupClientBehaviorDetailDefinition>();
                foreach (var scenario in setScenarioParameters.Scenarios)
                {
                    switch (scenario.ClientBehavior)
                    {
                        case ClientBehavior.Listen:
                            listen = Math.Max(listen, scenario.GetDetail<ClientBehaviorDetailDefinition>()?.Count ?? 0);
                            break;
                        case ClientBehavior.Echo:
                            var echo = scenario.GetDetail<ClientBehaviorDetailDefinition>();
                            if (echo != null && echo.Count > 0)
                            {
                                echoList.Add(echo);
                            }
                            break;
                        case ClientBehavior.P2P:
                            var p2p = scenario.GetDetail<ClientBehaviorDetailDefinition>();
                            if (p2p != null && p2p.Count > 0)
                            {
                                p2pList.Add(p2p);
                            }
                            break;
                        case ClientBehavior.Broadcast:
                            var broadcast = scenario.GetDetail<ClientBehaviorDetailDefinition>();
                            if (broadcast != null && broadcast.Count > 0)
                            {
                                broadcastList.Add(broadcast);
                            }
                            break;
                        case ClientBehavior.GroupBroadcast:
                            var groupBroadcast = scenario.GetDetail<GroupClientBehaviorDetailDefinition>();
                            if (groupBroadcast != null && groupBroadcast.Count > 0)
                            {
                                groupBroadcastList.Add(groupBroadcast);
                            }
                            break;
                        default:
                            break;
                    }
                }
            }

            private ClientAgentBehaviorSettings CreateSettings(
                int listen,
                List<ClientBehaviorDetailDefinition> echoList,
                List<ClientBehaviorDetailDefinition> p2pList,
                List<ClientBehaviorDetailDefinition> broadcastList,
                List<GroupClientBehaviorDetailDefinition> groupBroadcastList)
            {
                var result = new ClientAgentBehaviorSettings(listen);
                int nonListen = ScenarioState.totalConnected - listen;
                int current = 0;
                current = AddBehavior(
                    p2pList,
                    nonListen,
                    current,
                    (s, e, item) => result.AddP2P(s, e, item.MessageSize, ScenarioState.totalConnected, item.Interval));
                current = AddBehavior(
                    echoList,
                    nonListen,
                    current,
                    (s, e, item) => result.AddEcho(s, e, item.MessageSize, item.Interval));
                current = AddBehavior(
                    broadcastList,
                    nonListen,
                    current,
                    (s, e, item) => result.AddBroadcast(s, e, item.MessageSize, ScenarioState.totalConnected, item.Interval));
                current = AddBehavior(
                    groupBroadcastList,
                    nonListen,
                    current,
                    (s, e, item) =>
                    {
                        var gd = GroupDefinitions.Single(g => g.GroupFamily == item.GroupFamily);
                        result.AddGroup(ScenarioState.totalConnected,
                            s,
                            e,
                            item.MessageSize,
                            item.GroupFamily,
                            gd.GroupCount,
                            gd.GroupSize,
                            item.Interval);
                    });
                return result;
            }

            private static int AddBehavior<T>(
                List<T> list,
                int nonListen,
                int current,
                Action<int, int, T> add)
                where T : ClientBehaviorDetailDefinition
            {
                foreach (var item in list)
                {
                    if (item.Count == nonListen)
                    {
                        add(0, nonListen, item);
                        continue;
                    }

                    var end = current + item.Count;
                    if (end > nonListen)
                    {
                        add(current, nonListen, item);
                        add(0, end % nonListen, item);
                        current = end % nonListen;
                    }
                    else
                    {
                        add(current, end, item);
                        current = end;
                    }
                }

                return current;
            }
        }

        private sealed class ScenarioReadyState : ScenarioBaseState
        {
            public ScenarioReadyState(ScenarioState scenarioState,
                ClientAgentContainer clientAgentContainer, ClientAgentBehaviorSettings settings)
                : base(scenarioState)
            {
                ClientAgentContainer = clientAgentContainer;
                Settings = settings;
            }


            public ClientAgentContainer ClientAgentContainer { get; }

            public ClientAgentBehaviorSettings Settings { get; }

            public override void StartSenario(StartScenarioParameters startScenarioParameters)
            {
                var cts = new CancellationTokenSource();
                ClientAgentContainer.StartScenario(
                    index => Settings.GetClientAgentBehavior(ScenarioState.indexMap[index], GetLogger<IClientAgent>()), cts.Token);
                SetState(new RunningState(ScenarioState, ClientAgentContainer, Settings,
                    cts));
            }
        }

        private sealed class RunningState : ScenarioBaseState
        {
            public RunningState(
                ScenarioState scenarioState,
                ClientAgentContainer clientAgentContainer,
                ClientAgentBehaviorSettings settings,
                CancellationTokenSource cts)
                : base(scenarioState)
            {
                ClientAgentContainer = clientAgentContainer;
                Settings = settings;
                Cts = cts;
            }

            public ClientAgentContainer ClientAgentContainer { get; }

            public ClientAgentBehaviorSettings Settings { get; }

            public CancellationTokenSource Cts { get; }

            public override void StopSenario(StopScenarioParameters stopScenario)
            {
                Cts.Cancel();
                SetState(ScenarioState._save);
            }
        }
    }
}