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

        // Connection information
        public static readonly string ConnectionId = "information.ConnectionId";

        // Plugin parameters
        public static readonly string ConnectionStore = "Plugin.ConnectionStore";
        public static readonly string ConnectionOffset = "Plugin.ConnectionOffset";
        public static readonly string StatisticsStore = "Plugin.StatisticsStore";

        // Callback Name
        public static readonly string EchoCallbackName = "Echo";
        public static readonly string BroadcastCallbackName = "Broadcast";
        public static readonly string GetConnectionIdCallback = "GetConnectionId";

        // Message payload
        public static readonly string Timestamp = "payload.Timestamp";
        public static readonly string MessageBlob = "payload.MessageBlob";

        // Timer
        public static readonly string Timer = "Timer";

        // Statistics
        public static readonly string StatisticsMessageSent = "message:sent";
    }
}
