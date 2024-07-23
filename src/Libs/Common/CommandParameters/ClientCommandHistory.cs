using System.Collections.Generic;

namespace Azure.SignalRBench.Common
{
    public class ClientCommandHistory
    {
        public IList<ClientCommandRound> Histories = new List<ClientCommandRound>();
    }

    public class ClientCommandRound
    {
        public Dictionary<string, SetClientRangeParameters> SetClientRangeParameters =
            new Dictionary<string, SetClientRangeParameters>();
        public StartClientConnectionsParameters StartClientConnectionsParameters;
        public SetScenarioParameters SetScenarioParameters;
        public StartScenarioParameters StartScenarioParameters;
        public StopScenarioParameters StopScenarioParameters;
    }
}