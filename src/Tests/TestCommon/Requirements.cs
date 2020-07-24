// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Azure.SignalRBench.Tests
{
    public static class Requirements
    {
        public static string RequireRedis()
        {
            var redis = TestConfiguration.Instance.Redis;
            Skip.If(string.IsNullOrEmpty(redis), "Redis is required.");
            return redis;
        }

        public static string RequireStorage()
        {
            var storage = TestConfiguration.Instance.Storage;
            Skip.If(string.IsNullOrEmpty(storage), "Storage is required.");
            return storage;
        }
    }
}
