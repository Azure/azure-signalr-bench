using Azure.SignalRBench.Common;

namespace Azure.SignalRBench.Coordinator.Entities
{
    public class RoundStatus : ReportClientStatusParameters
    {
        public int ActiveConnection { get; set; }
        public int RoundConnected { get; set; }

        public bool Check()
        {
            //Test , impose a strict condition
            if (ReconnectingCount > 0)
            {
                return false;
            }

            if (TotalReconnectCount > 0)
            {
                return false;
            }

            if (MessageRecieved < ExpectedRecievedMessageCount)
            {
                return false;
            }

            if (ConnectedCount < RoundConnected)
            {
                return false;
            }

            if (Latency[LatencyClass.LessThan2s]>0)
            {
                return false;
            }
            
            if (Latency[LatencyClass.LessThan5s]>0)
            {
                return false;
            }
            
            if (Latency[LatencyClass.MoreThan5s]>0)
            {
                return false;
            }
            
            return true;
        }
        
    }
}