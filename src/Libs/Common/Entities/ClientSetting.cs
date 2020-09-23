// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Azure.SignalRBench.Common
{
    public class ClientSetting
    {
        public ClientBehavior Behavior { get; set; }
        public int IntervalInMilliseconds { get; set; }
        public int Count { get; set; }
        public string? GroupFamily { get; set; }
    }
}
