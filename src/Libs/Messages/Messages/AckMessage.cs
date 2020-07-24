// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Azure.SignalRBench.Messages
{
    public class AckMessage : Message
    {
        [JsonProperty(Required = Required.Always)]
        public int AckId { get; set; }

        [JsonProperty(Required = Required.Always)]
        public bool IsCompleted { get; set; }

        public double? Progress { get; set; }
    }
}
