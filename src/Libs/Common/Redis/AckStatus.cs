// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Azure.SignalRBench.Messages
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AckStatus
    {
        Running,
        Completed,
        Faulted,
        Canceled,
    }
}
