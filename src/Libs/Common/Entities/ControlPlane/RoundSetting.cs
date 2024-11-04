// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Azure.SignalRBench.Common
{
    public class RoundSetting
    {
        public int DurationInSeconds { get; set; }
        public ClientSetting[] ClientSettings { get; set; } = Array.Empty<ClientSetting>();
    }
}
