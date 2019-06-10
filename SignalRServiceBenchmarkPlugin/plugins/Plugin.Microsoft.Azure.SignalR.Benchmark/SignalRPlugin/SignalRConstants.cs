using Microsoft.Azure.SignalR.PerfTest.AppServer;
using System;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    public static class SignalRConstants
    {
        // Configuration parameters
        public static readonly string Type = "Type";
        public static readonly string ConcurrentConnection = "Parameter.ConcurrentConnection";
        public static readonly string BatchMode = "Parameter.BatchMode";
        public static readonly string BatchWait = "Parameter.BatchWait";
        public static readonly string ConnectionTotal = "Parameter.ConnectionTotal";
        public static readonly string ConnectionBegin = "Parameter.ConnectionBegin";
        public static readonly string ConnectionEnd = "Parameter.ConnectionEnd";
        public static readonly string HubUrls = "Parameter.HubUrl";
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
        public static readonly string StatisticsOutputPath = "Parameter.StatisticsOutputPath";
        public static readonly string CriteriaMaxFailConnectionPercentage = "Parameter.CriteriaMaxFailConnectionPercentage";
        public static readonly string CriteriaMaxFailConnectionAmount = "Parameter.CriteriaMaxFailConnectionAmount";
        public static readonly string CriteriaMaxFailSendingPercentage = "Parameter.CriteriaMaxFailSendingPercentage";
        public static readonly string LatencyStep = "Parameter.LatencyStep";
        public static readonly string LatencyMax = "Parameter.LatencyMax";
        public static readonly string GroupConfigMode = "Parameter.Mode";
        public static readonly string PercentileList = "Parameter.PercentileList";
        public static readonly string ActionAfterConnect = "Parameter.ActionAfterConnect";
        public static readonly string HttpClientManager = "Parameter.HttpClientManager";
        public static readonly string StatPrintMode = "Parameter.StatPrintMode";

        // Connection/group information
        public static readonly string ConnectionId = "information.ConnectionId";
        public static readonly string GroupName = "information.GroupName";
        public static readonly string ConnectionIndex = "information.ConnectionIndex";
        public static readonly string ConnectionSuccessFlag = "information.ConnectionSuccess";

        // Plugin parameters

        public static readonly string ConnectionIdStore = "Plugin.ConnectionId";
        public static readonly string ConnectionOffset = "Plugin.ConnectionOffset";
        public static readonly string ConnectionStore = "Plugin.ConnectionStore";
        public static readonly string ConnectionString = "Plugin.ConnectionString";
        public static readonly string ConnectionType = "Plugin.ConnectionType";
        public static readonly string RegisteredCallbacks = "Plugin.Callback";
        public static readonly string StatisticsStore = "Plugin.StatisticsStore";
        public static readonly string RepairConnectionCTS = "Plugin.RepairConnectionCTS";

        // Callback Name
        public static readonly string EchoCallbackName = "Echo";
        public static readonly string BroadcastCallbackName = "Broadcast";
        public static readonly string SendToClientCallbackName = "SendToClient";
        public static readonly string SendToGroupCallbackName = "SendToGroup";
        public static readonly string RecordLatencyCallbackName = "RecordLatency";
        public static readonly string JoinGroupCallbackName = "JoinGroup";
        public static readonly string LeaveGroupCallbackName = "LeaveGroup";
        public static readonly string GetConnectionIdCallback = "GetConnectionId";
        public static readonly string ConnectionIdCallback = "ConnectionId";
        public static readonly string OnConnectedCallback = "OnConnected";

        // Message payload
        public static readonly string Timestamp = "payload.Timestamp";
        public static readonly string MessageBlob = "payload.MessageBlob";

        public const long LATENCY_STEP = 100;
        public const long LATENCY_MAX = 1000;
        public const string PERCENTILE_LIST = "0.5,0.9,0.95,0.99";
        // Stop collector
        public static readonly string StopCollector = "StopCollector";

        public static readonly string LocalhostAppServer = "LocalhostAppServer";
        public const int LocalhostAppServerPort = 54321;
        public static readonly string LocalhostUrl = $"http://localhost:{LocalhostAppServerPort}{Startup.HUB_NAME}";
        // Negotiation server for REST API clients
        public static readonly int NegoatiationServerPort = 12345;
        
        public static readonly string NegotiationServer = "NegotiationServer";
        // REST API: http://url:port/{hub}?user={userId}
        public static readonly string NegotiationUrl = $"http://localhost:{NegoatiationServerPort}";

        // Statistics
        public static readonly string StatisticsTimestamp = "Time";
        public static readonly string StatisticsCounters = "Counters";
        public const string StatisticsEpoch = "epoch";
        public const string StatisticsSendingStep = "sendingStep";
        public const string StatisticsMessageSent = "message:sent";
        public const string StatisticsMessageSentSize = "message:sentSize";
        public const string StatisticsMessageReceived = "message:received";
        public const string StatisticsMessageReceivedSize = "message:recvSize";
        public const string StatisticsGroupJoinSuccess = "group:join:success";
        public const string StatisticsGroupLeaveSuccess = "group:leave:success";
        public const string StatisticsGroupJoinFail = "group:join:fail";
        public const string StatisticsGroupLeaveFail = "group:leave:fail";
        public const string StatisticsConnectionConnectSuccess = "connection:connect:success";
        public const string StatisticsConnectionConnectFail = "connection:connect:fail";
        public const string StatisticsConnectionReconnect = "connection:connect:reconnect";
        public const string StatisticsConnectionInit = "connection:connect:init";
        public const string StatisticsConnectionLifeSpan = "connection:connect:lifespan";
        public const string StatisticsConnectionCost = "connection:connect:cost";
        public const string StatisticsConnectionReconnectCost = "connection:reconnect:cost";
        public const string StatisticsConnectionSLA = "connection:sla";
        public const string StatisticsLatencyLessThan = "message:lt:";
        public const string StatisticsLatencyGreatEqThan = "message:ge:";
        // Constants
        public static readonly int ConnectionCloseTimeout = 100;

        // Default RPC task timeout if there is no specific duration
        public static readonly long MillisecondsToWait = 600000;

        // Default batch mode: "LimitRatePress"
        public static readonly string DefaultBatchMode = SignalREnums.BatchMode.HighPress.ToString();

        // Default interval (milliseconds) for batch process
        public static readonly int BatchProcessDefaultWait = 1000;

        // Default granularity for rate limit processing (milliseconds)
        public static readonly int RateLimitDefaultGranularity = 100;

        // Default cancellation token timeout
        public static readonly TimeSpan DefaultCancellationToken = TimeSpan.FromSeconds(5);

        public static readonly string DefaultRestHubName = "restbenchhub";

        public static readonly string DefaultClientUserIdPrefix = "cli";

        // Set the ServicePointManager.DefaultConnectionLimit for serverless test
        public static readonly int DefaultConnectionLimit = 500;

        public static readonly string HandlerLifetimeKey = "HandlerLifetimeKey";
    }
}
