// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Azure.SignalRBench.Common
{
    public class StartClientConnectionsParameters
    {
        public double Rate { get; set; }
        public Protocol Protocol { get; set; }
        public bool IsAnonymous { get; set; }
        public string Url { get; set; } = string.Empty;
        public ClientLifetimeDefinition ClientLifetime { get; set; } = new ClientLifetimeDefinition();
        public GroupDefinition[] GroupDefinitions { get; set; } = Array.Empty<GroupDefinition>();
    }
}
