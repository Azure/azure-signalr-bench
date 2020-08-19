// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Azure.SignalRBench.Common
{
    public class GroupClientBehaviorDetailDefinition : ClientBehaviorDetailDefinition
    {
        public string GroupFamily { get; set; }
        public int GroupCount { get; set; }
        public int GroupSize { get; set; }
    }
}
