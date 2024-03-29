﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Azure.SignalRBench.Common
{
    public class GroupDefinition
    {
        public string GroupFamily { get; set; } = string.Empty;
        public int GroupCount { get; set; }
        public int GroupSize { get; set; }
    }
}
