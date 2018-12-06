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
            Fail,
            Reconnect
        }
        
        public enum GroupConfigMode
        {
            Group,
            Connection
        }

        public enum ClientType
        {
            AspNetCore,
            AspNet
        }
    }
}
