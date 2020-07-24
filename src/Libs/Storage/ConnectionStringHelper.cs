// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;

namespace Azure.SignalRBench.Storage
{
    internal static class ConnectionStringHelper
    {
        public static string GetAccountKeyFromConnectionString(string connectionString)
        {
            var csBuilder = new DbConnectionStringBuilder { ConnectionString = connectionString };
            return (string)csBuilder["AccountKey"];
        }
    }
}
