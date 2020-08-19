// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Azure.SignalRBench.Common
{
    public class ClientLifetimeDefinition
    {
        public int MaxLifetimeInSeconds { get; set; }
        public int MinLifetimeInSeconds { get; set; }
    }
}
