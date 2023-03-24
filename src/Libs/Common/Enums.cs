// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Azure.SignalRBench.Common
{
    /// <summary>
    /// Get HttpTransportType by <code>(HttpTransportType)((int)protocol & 0xF)</code>
    /// Get TransferFormat by <code>(TransferFormat)((int)protocol >> 4)</code>
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Protocol
    {
        WebSocketsWithMessagePack,
        WebSocketsWithJson,
        ServerSideEventsWithJson,
        LongPollingWithMessagePack,
        LongPollingWithJson,
        RawWebSocketJson,
        RawWebSocketReliableJson,
        RawWebSocketReliableProtobuf
    }

    public static class ProtocolExtentions
    {
        public static string GetFormatProtocol(this Protocol protocol)
        {
            return protocol.ToString().ToLower().Contains("json") ? "json" : "messagepack";
        }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ClientBehavior
    {
        Listen,
        Echo,
        Broadcast,
        GroupBroadcast,
        P2P
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum LatencyClass
    {
        LessThan50ms,
        LessThan100ms,
        LessThan200ms,
        LessThan500ms,
        LessThan1s,
        LessThan2s,
        LessThan5s,
        MoreThan5s,
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum TestCategory
    {
        AspnetCoreSignalR,
        AspnetCoreSignalRServerless,
        AspnetSignalR,
        RawWebsocket,
    }

    public enum TestState
    {
        InProgress,
        Failed,
        Finished
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ServiceName
    {
        SignalR,
        RawWebsocket,
    }

    public enum SignalRServiceMode
    {
        Default,
        Serverless,
    }
}