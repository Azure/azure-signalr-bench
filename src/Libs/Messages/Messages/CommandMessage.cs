// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Azure.SignalRBench.Messages
{
    public class CommandMessage : Message
    {
        [JsonProperty(Required = Required.Always)]
        public string Command { get; set; }

        [JsonProperty(Required = Required.Always)]
        public int AckId { get; set; }

        public JObject Parameters { get; set; }
    }
}
