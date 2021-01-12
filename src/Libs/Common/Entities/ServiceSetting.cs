// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Azure.SignalRBench.Common
{
    public class ServiceSetting
    {
        public string? AsrsConnectionString { get; set; }
        public string? Location { get; set; }
        public string? Tier { get; set; }
        public int? Size { get; set; }
        public string Env { get; set; }
        public string Tags { get; set; }
    }
}
