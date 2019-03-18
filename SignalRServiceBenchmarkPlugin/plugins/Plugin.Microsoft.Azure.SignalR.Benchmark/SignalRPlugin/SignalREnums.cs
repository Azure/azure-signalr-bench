using System;
using System.Collections.Generic;
using System.Text;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    public static class SignalREnums
    {
        public enum ConnectionState 
        {
            Init,
            Success,
            Fail
        }
        
        public enum GroupConfigMode
        {
            Group, // select connections from every group
            Connection // randomly select the connections
        }

        public enum ClientType
        {
            AspNetCore,
            AspNet,
            DirectConnect
        }

        public enum BatchMode
        {
            LowPress,
            HighPress,
            LimitRatePress,
            ExtLimitRatePress
        }
    }
}
