// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Azure.SignalRBench.Storage
{
    internal static class DelayHelper
    {
        public static IEnumerable<TimeSpan> GetDelaySequence() =>
            GetDelaySequence(TimeSpan.FromSeconds(10));

        public static IEnumerable<TimeSpan> GetDelaySequence(TimeSpan maxSpan)
        {
            int delay = 100;
            while (TimeSpan.FromMilliseconds(delay) < maxSpan)
            {
                yield return TimeSpan.FromMilliseconds(delay);
                delay = delay * 7 / 10;
            }
            while (true)
            {
                yield return maxSpan;
            }
        }
    }
}
