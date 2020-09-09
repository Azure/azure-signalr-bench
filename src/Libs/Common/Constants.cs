// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Azure.SignalRBench.Common
{
    public static class Constants
    {
        public static class KeyVaultKey
        {
            public const string StorageConnectionString = "sa-accessKey";
        }

        public static class EnvVariableKey
        {
            public const string StorageConnectionStringKey = "Storage:SignalR:ConnectionString";
            public const string RedisConnectionStringKey = "Redis:SignalR:ConnectionString";
            public const string PodNameStringKey = "Podname";
        }
    }
}
