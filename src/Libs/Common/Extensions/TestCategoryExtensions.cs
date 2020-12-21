// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Azure.SignalRBench.Common
{
    public static class TestCategoryExtensions
    {
        public static SignalRServiceMode GetServiceMode(this TestCategory category)
        {
            switch (category)
            {
                case TestCategory.AspnetCoreSignalR:
                case TestCategory.AspnetSignalR:
                    return SignalRServiceMode.Default;
                case TestCategory.AspnetCoreSignalRServerless:
                    return SignalRServiceMode.Serverless;
                default:
                    throw new ArgumentOutOfRangeException(nameof(category));
            }
        }
    }
}
