// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Azure.SignalRBench.Common
{
    public class ScenarioSetting
    {
        public int TotalConnectionCount { get; set; }
        public GroupDefinition[] GroupDefinitions { get; set; } = Array.Empty<GroupDefinition>();
        public RoundSetting[] Rounds { get; set; } = Array.Empty<RoundSetting>();
    }
}
