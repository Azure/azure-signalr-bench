using System;
using Azure.SignalRBench.Common;

namespace Azure.SignalRBench.Coordinator.Entities
{
    public class RoundStatus : ReportClientStatusParameters
    {
        public int ActiveConnection { get; set; }
        public int RoundConnected { get; set; }

        private static double _pencent = 0.05;

        public bool Check()
        {
            Console.WriteLine("New check method");
            //Test , impose a strict condition
            var threshold = RoundConnected * _pencent;
            if (ReconnectingCount > threshold)
            {
                Console.WriteLine("Reconnect check fail");
                return false;
            }

            // if (TotalReconnectCount > threshold)
            // {
            //     return false;
            // }

            if (MessageRecieved < ExpectedRecievedMessageCount * (1 - _pencent) || MessageRecieved == 0)
            {
                Console.WriteLine("MessageRecieved check fail");
                return false;
            }

            if (ConnectedCount < RoundConnected * 0.75)
            {
                Console.WriteLine("ConnectedCount check fail");
                return false;
            }

            if (Latency[LatencyClass.LessThan2s] + Latency[LatencyClass.LessThan5s] + Latency[LatencyClass.MoreThan5s] >
                MessageRecieved * _pencent)
            {
                Console.WriteLine("Latency check fail");
                return false;
            }

            return true;
        }
    }
}