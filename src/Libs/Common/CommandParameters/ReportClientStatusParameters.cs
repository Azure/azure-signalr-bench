// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Azure.SignalRBench.Common
{
    public class ReportClientStatusParameters
    {
        public int ConnectedCount { get; set; }
        public int ConnectingCount { get; set; }
        public int MessageSent { get; set; }
        public int MessageRecieved { get; set; }
        public Dictionary<int, int> Latency { get; set; }
    }
}
