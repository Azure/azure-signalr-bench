using Azure.SignalRBench.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Azure.SignalRBench.Client
{
    public class ScenarioState : IScenarioState
    {
        private ClientAgentContainer clientAgentContainer;
        private SetClientRangeParameters _setClientRangeParameters;
        private SetScenarioParameters _setScenarioParameters;

        public void SetClientRange(SetClientRangeParameters setClientRangeParameters)
        {
            throw new NotImplementedException();
        }

        public void SetSenario(SetScenarioParameters setScenarioParameters)
        {
            throw new NotImplementedException();
        }

        public Task StartClientConnections(StartClientConnectionsParameters startClientConnectionsParameters)
        {
            throw new NotImplementedException();
        }

        public Task StartSenario(StartScenarioParameters startScenarioParameters)
        {
            throw new NotImplementedException();
        }

        public Task StopClientConnections(StopClientConnectionsParameters StopClientConnectionsParameters)
        {
            throw new NotImplementedException();
        }

        public Task StopSenario(StopScenarioParameters stopScenario)
        {
            throw new NotImplementedException();
        }
    }
}
