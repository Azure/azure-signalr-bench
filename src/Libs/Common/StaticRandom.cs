// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Azure.SignalRBench.Common
{
    public static class StaticRandom
    {
        private static readonly Random _random = new Random();

        public static double NextDouble()
        {
            lock (_random)
            {
                return _random.NextDouble();
            }
        }

        public static int Next()
        {
            lock (_random)
            {
                return _random.Next();
            }
        }

        public static int Next(int maxValue)
        {
            lock (_random)
            {
                return _random.Next(maxValue);
            }
        }

        public static int Next(int minValue, int maxValue)
        {
            lock (_random)
            {
                return _random.Next(minValue, maxValue);
            }
        }
    }
}
