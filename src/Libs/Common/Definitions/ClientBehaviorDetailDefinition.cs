// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Azure.SignalRBench.Common
{
    public class ClientBehaviorDetailDefinition
    {
        public int Count { get; set; }

        public TimeSpan Interval { get; set; }

        public int MessageSize { get; set; }
    }
}
