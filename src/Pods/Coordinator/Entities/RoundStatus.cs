using Azure.SignalRBench.Common;

namespace Azure.SignalRBench.Coordinator.Entities
{
    public class RoundStatus : ReportClientStatusParameters
    {
        public int ActiveConnection { get; set; }
        public int RoundConnected { get; set; }

        private static double _pencent = 0.001;

        public bool Check()
        {
            //Test , impose a strict condition
            var threshold = RoundConnected*_pencent;
            if (ReconnectingCount > threshold)
            {
                return false;
            }

            if (TotalReconnectCount > threshold)
            {
                return false;
            }

            if (MessageRecieved < ExpectedRecievedMessageCount*(1-_pencent))
            {
                return false;
            }

            if (ConnectedCount < RoundConnected*(1-_pencent))
            {
                return false;
            }

            if (Latency[LatencyClass.LessThan2s]>MessageRecieved*_pencent)
            {
                return false;
            }
            
            if (Latency[LatencyClass.LessThan5s]>MessageRecieved*_pencent)
            {
                return false;
            }
            
            if (Latency[LatencyClass.MoreThan5s]>MessageRecieved*_pencent)
            {
                return false;
            }
            
            return true;
        }
        
    }
}