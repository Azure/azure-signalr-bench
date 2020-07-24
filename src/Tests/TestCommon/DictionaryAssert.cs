// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

using Xunit;

namespace Azure.SignalRBench.Tests
{
    public static class DictionaryAssert
    {
        public static void Equal<TKey, TValue>(IDictionary<TKey, TValue> expected, IDictionary<TKey, TValue> actual) =>
            Assert.Equal(
                expected.OrderBy(p => p.Key).Select(p => (p.Key, p.Value)),
                actual.OrderBy(p => p.Key).Select(p => (p.Key, p.Value)));
    }
}
