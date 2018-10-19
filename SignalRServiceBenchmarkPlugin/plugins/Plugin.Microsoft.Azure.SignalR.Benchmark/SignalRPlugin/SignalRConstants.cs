﻿using System;
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
        public static readonly string GroupCount = "Parameter.GroupCount";
        public static readonly string GroupLevelRemainderBegin = "Parameter.GroupLevelRemainderBegin";
        public static readonly string GroupLevelRemainderEnd = "Parameter.GroupLevelRemainderEnd";
        public static readonly string GroupInternalRemainderBegin = "Parameter.GroupInternalRemainderBegin";
        public static readonly string GroupInternalRemainderEnd = "Parameter.GroupInternalRemainderEnd";
        public static readonly string GroupInternalModulo = "Parameter.GroupInternalModulo";

        // Connection/group information
        public static readonly string ConnectionId = "information.ConnectionId";
        public static readonly string GroupName = "information.GroupName";

        // Plugin parameters
        public static readonly string ConnectionStore = "Plugin.ConnectionStore";
        public static readonly string ConnectionOffset = "Plugin.ConnectionOffset";
        public static readonly string StatisticsStore = "Plugin.StatisticsStore";
        public static readonly string ConnectionIdStore = "Plugin.ConnectionId";

        // Callback Name
        public static readonly string EchoCallbackName = "Echo";
        public static readonly string BroadcastCallbackName = "Broadcast";
        public static readonly string SendToClientCallbackName = "SendToClient";
        public static readonly string SendToGroupCallbackName = "SendToGroup";
        public static readonly string JoinGroupCallbackName = "JoinGroup";
        public static readonly string GetConnectionIdCallback = "GetConnectionId";

        // Message payload
        public static readonly string Timestamp = "payload.Timestamp";
        public static readonly string MessageBlob = "payload.MessageBlob";

        // Timer
        public static readonly string Timer = "Timer";

        // Statistics
        public static readonly string StatisticsMessageSent = "message:sent";
        public static readonly string StatisticsGroupJoinSuccess = "group:success";
        public static readonly string StatisticsGroupJoinFail = "group:fail";

    }
}