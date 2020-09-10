using Azure.SignalRBench.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Azure.SignalRBench.Client
{
    public interface IScenarioState
    {
        public void SetSenario(SetScenarioParameters setScenarioParameters);

        public Task StartSenario(StartScenarioParameters startScenarioParameters);

        public Task StopSenario(StopScenarioParameters stopScenario);

        public void SetClientRange(SetClientRangeParameters setClientRangeParameters);

        public Task StartClientConnections(StartClientConnectionsParameters startClientConnectionsParameters);

        public Task StopClientConnections(StopClientConnectionsParameters StopClientConnectionsParameters);

    }
}
