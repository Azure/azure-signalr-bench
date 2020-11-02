﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Azure.SignalRBench.Common
{
    public static class Constants
    {
        public static class KeyVaultKeys
        {
            public const string StorageConnectionStringKey = "sa-accessKey";
            public const string PrefixKey = "prefix";
            public const string SubscriptionKey = "subscription";
            public const string LocationKey = "location";
            public const string CloudKey = "cloud";
            public const string ServicePrincipalKey = "service-principal";
            public const string KubeConfigKey = "kube-config";
        }

        public static class ConfigurationKeys
        {
            public const string KeyVaultUrlKey = "kvUrl";
            public const string TestIdKey = "testId";
            public const string StorageConnectionStringKey = "storage";
            public const string RedisConnectionStringKey = "redis";
            public const string PodNameStringKey = "Podname";
            public const string ConnectionString = "connectionString";
            public const string AppServerUrl = "appServerUrl";
        }

        public static class QueueNames
        {
            public const string PortalJob = "portal-job";
        }
    }
}