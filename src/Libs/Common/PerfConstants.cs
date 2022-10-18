// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Azure.SignalRBench.Common
{
    public static class PerfConstants
    {
        public static class KeyVaultKeys
        {
            public const string StorageConnectionStringKey = "sa-accessKey";
            public const string PrefixKey = "prefix";
            public const string SubscriptionKey = "subscription";
            public const string PPESubscriptionKey = "ppe-subscription";
            public const string LocationKey = "location";
            public const string PPELocationKey = "ppe-location";
            public const string EncryptCert = "authEncrypt";
            public const string CloudKey = "cloud";
            public const string ServicePrincipalKey = "service-principal";
            public const string PPEServicePrincipalKey = "ppe-service-principal";

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
            public const string ConnectionNum = "connectionNum";
            public const string MsiAppId = "msiAppId";
            public const string PerfV2 = "perfv2";
            public const string DomainKey = "domain";
            public const string TestCategory = "testCategory";
            public const string Protocol = "protocol";
            public const string Image = "Image";
            public const string Internal = "internal";
        }

        public static class TableNames
        {
            public const string TestConfig = "testConfig";
            public const string TestStatus = "testStatus";
            public const string UserIdentity = "userIdentity";
        }

        public static class QueueNames
        {
            public const string PortalJob = "portal-job";
        }

        public static class Roles
        {
            public const string Contributor = "Contributor";
            public const string Pipeline = "Pipeline";
        }

        public static class Policy
        {
            public const string RoleLogin = "RoleLogin";
        }

        public static class AuthSchema
        {
            public const string BasicAuth = "BasicAuthentication";
        }

        public static class Number
        {
            public const int ConnectionsPerClient = 5000;
        }

        public static class Name
        {
            public const string HubName = "signalrbench";
            public const string OsLabel = "kubernetes.io/os";
            public const string Windows = "windows";
            public const string Linux = "linux";
        }

        public static class Cloud
        {
            public const string AzureGlobal = "AzureGlobal";
            public const string PPE = "PPE";
        }
    }
}