using Azure.SignalRBench.Common;

namespace Azure.SignalRBench.Coordinator.Entities
{
    public class RoundStatus :ReportClientStatusParameters
    {
        public int ActiveConnection { get; set; }
        public int RoundConnected { get; set; }
    }
}