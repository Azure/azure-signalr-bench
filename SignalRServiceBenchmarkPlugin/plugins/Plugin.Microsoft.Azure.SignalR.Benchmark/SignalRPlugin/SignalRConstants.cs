using System;
using System.Collections.Generic;
using System.Text;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    public static class SignalRConstants
    {
        // Configuration parameters
        public static readonly string Type = "Type";
        public static readonly string ConcurrentConnection = "Parameter.ConcurrentConnetion";
        public static readonly string ConnectionTotal = "Parameter.ConnectionTotal";
        public static readonly string ConnectionBegin = "Parameter.ConnectionBegin";
        public static readonly string ConnectionEnd = "Parameter.ConnectionEnd";
        public static readonly string HubUrl = "Parameter.HubUrl";
        public static readonly string HubProtocol = "Parameter.Protocol";
        public static readonly string TransportType = "Parameter.TransportType";
        public static readonly string Duration = "Parameter.Duration";
        public static readonly string Interval = "Parameter.Interval";
        public static readonly string RemainderBegin = "Parameter.RemainderBegin";
        public static readonly string RemainderEnd = "Parameter.RemainderEnd";
        public static readonly string Modulo = "Parameter.Modulo";
        public static readonly string MessageSize = "Parameter.MessageSize";


        // Plugin parameters
        public static readonly string ConnectionStore = "Plugin.ConnectionStore";
        public static readonly string ConnectionOffset = "Plugin.ConnectionOffset";

        // Callback Name
        public static readonly string EchoCallbackName = "Echo";

        // Message payload
        public static readonly string Timestamp = "Payload.Timestamp";
        public static readonly string MessageBlob = "Payload.MessageBlob";
    }
}
