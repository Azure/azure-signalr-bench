// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Storage;
using System.Data.Common;

namespace Azure.SignalRBench.Storage
{
    internal static class ConnectionStringHelper
    {
        public static (string AccountName, string AccountKey, string EndpointSuffix) ParseConnectionString(string connectionString)
        {
            var csBuilder = new DbConnectionStringBuilder { ConnectionString = connectionString };
            return ((string)csBuilder["AccountName"], (string)csBuilder["AccountKey"], (string)csBuilder["EndpointSuffix"]);
        }

        public static StorageSharedKeyCredential GetCredential(string connectionString)
        {
            var csBuilder = new DbConnectionStringBuilder { ConnectionString = connectionString };
            return new StorageSharedKeyCredential((string)csBuilder["AccountName"], (string)csBuilder["AccountKey"]);
        }
    }
}
