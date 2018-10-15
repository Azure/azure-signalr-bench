using System;
using System.Collections.Generic;
using System.Text;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    public static class SignalRConstants
    {
        // Configuration parameters
        public static readonly string ConcurrentConnection = "Parameter.ConcurrentConnetion";
        public static readonly string ConnectionTotal = "Parameter.ConnectionTotal";
        public static readonly string ConnectionBegin = "Parameter.ConnectionBegin";
        public static readonly string ConnectionEnd = "Parameter.ConnectionEnd";
        public static readonly string HubUrl = "Parameter.HubUrl";
        public static readonly string HubProtocol = "Parameter.Protocol";
        public static readonly string TransportType = "Parameter.TransportType";
        public static readonly string Type = "Type";

        // Plugin parameters
        public static readonly string ConnectionStore = "Plugin.ConnectionStore";
        public static readonly string ConnectionOffset = "Plugin.ConnectionOffset";

    }
}
