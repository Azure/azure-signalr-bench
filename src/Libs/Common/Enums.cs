// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Azure.SignalRBench.Common
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SignalRProtocol
    {
        WebSocketWithMessagePack,
        WebSocketWithJson,
        ServerSideEventWithJson,
        LongPollingWithMessagePack,
        LongPollingWithJson,
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ClientBehavior
    {
        Listen,
        Echo,
        Broadcast,
        GroupBroadcast,
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
    }
}
