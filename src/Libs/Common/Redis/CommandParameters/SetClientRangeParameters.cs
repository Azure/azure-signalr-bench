// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Azure.SignalRBench.Common
{
    public class SetClientRangeParameters
    {
        public int StartIdTruncated { get; set; }
        public int LocalCountDelta { get; set; }
        public int TotalCountDelta { get; set; }
    }
}
