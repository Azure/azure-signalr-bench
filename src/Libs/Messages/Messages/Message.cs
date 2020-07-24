// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Azure.SignalRBench.Messages
{
    public abstract class Message
    {
        [JsonProperty(Required = Required.Always)]
        public string Sender { get; set; }
    }
}
