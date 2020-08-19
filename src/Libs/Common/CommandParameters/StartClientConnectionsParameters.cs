// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Azure.SignalRBench.Common
{
    public class StartClientConnectionsParameters
    {
        public int TotalCount { get; set; }
        public int Rate { get; set; }
        public SignalRProtocol Protocol { get; set; }
        public bool IsAnonymous { get; set; }
    }
}
