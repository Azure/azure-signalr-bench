// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Azure.SignalRBench.Messages
{
    public class RpcFaultedException : Exception
    {
        public RpcFaultedException(string message) : base(message) { }
    }
}
