using Azure.SignalRBench.Common;
using System.Threading.Tasks;

namespace Azure.SignalRBench.Client
{
    public interface IScenarioState
    {
        public void SetClientRange(SetClientRangeParameters setClientRangeParameters);

        public Task StartClientConnections(StartClientConnectionsParameters startClientConnectionsParameters);

        public void SetSenario(SetScenarioParameters setScenarioParameters);

        public void StartSenario(StartScenarioParameters startScenarioParameters);

        public void StopSenario(StopScenarioParameters stopScenario);

        public Task StopClientConnections(StopClientConnectionsParameters StopClientConnectionsParameters);

    }
}
