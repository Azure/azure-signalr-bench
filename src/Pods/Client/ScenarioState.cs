using Azure.SignalRBench.Client.Exceptions;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.SignalRBench.Client
{
    public class ScenarioState : IScenarioState
    {
        private ClientAgentContainer clientAgentContainer;
        private volatile SetClientRangeParameters _setClientRangeParameters;
        private volatile SetScenarioParameters _setScenarioParameters;
        private CancellationTokenSource cts;
        private volatile State state = State.Uninited;
        private int[] localConnectionIDs;
        public void SetClientRange(SetClientRangeParameters setClientRangeParameters)
        {
            if (state != State.Uninited)
                throw new InvalidScenarioStateException();
            _setClientRangeParameters = setClientRangeParameters;
            state = State.ClientRangeSet;
        }

        public void SetSenario(SetScenarioParameters setScenarioParameters)
        {
            if (state != State.ClientRangeSet)
                throw new InvalidScenarioStateException();
            _setScenarioParameters = setScenarioParameters;
            state = State.SceneriaSet;
        }

        public async Task StartClientConnections(StartClientConnectionsParameters startClientConnectionsParameters)
        {
            if (state != State.SceneriaSet)
                throw new InvalidScenarioStateException();
            cts = new CancellationTokenSource();
            clientAgentContainer = new ClientAgentContainer(_setClientRangeParameters, startClientConnectionsParameters.Protocol,
                startClientConnectionsParameters.IsAnonymous, startClientConnectionsParameters.Url);
            clientAgentContainer.ScheduleReportedStatus(cts);
            await clientAgentContainer.StartAsync(startClientConnectionsParameters.Rate, _setScenarioParameters, cts.Token);
            localConnectionIDs = Util.GenerateConnectionID(startClientConnectionsParameters.TotalCount, _setClientRangeParameters.StartId, _setClientRangeParameters.Count);
            state = State.ConnectionEstablished;
        }

        public async Task StartSenario(StartScenarioParameters startScenarioParameters)
        {
            if (state != State.ConnectionEstablished)
                throw new InvalidScenarioStateException();
            var count = _setClientRangeParameters.Count;
            var scenario = _setScenarioParameters.Scenarios[startScenarioParameters.index];
            var connections = from i in Enumerable.Range(0, count)
                              where localConnectionIDs[i] < scenario.GetDetail<ClientBehaviorDetailDefinition>()?.Count
                              select i;
            await clientAgentContainer.StartScenario(connections, scenario, cts.Token);
        }

        public async Task StopClientConnections(StopClientConnectionsParameters stopClientConnectionsParameters)
        {
            if (state != State.ConnectionEstablished)
                throw new InvalidScenarioStateException();
            cts.Cancel();
            await clientAgentContainer.StopAsync(stopClientConnectionsParameters.Rate);
            state = State.SceneriaSet;
        }

        public Task StopSenario(StopScenarioParameters stopScenario)
        {
            cts.Cancel();
            return Task.CompletedTask;
        }
    }

    internal enum State
    {
        Uninited, ClientRangeSet, SceneriaSet, ConnectionEstablished
    }
}
