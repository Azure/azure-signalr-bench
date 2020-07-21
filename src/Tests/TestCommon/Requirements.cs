// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.Azure.SignalRBench.Tests
{
    public static class Requirements
    {
        public static string RequireRedis()
        {
            var redis = TestConfiguration.Instance.Redis;
            Skip.If(string.IsNullOrEmpty(redis), "Redis is required.");
            return redis;
        }
    }
}
