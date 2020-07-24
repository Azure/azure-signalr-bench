// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace Azure.SignalRBench.Storage
{
    public class PerfStorageProvider
    {
        private readonly TokenCredential _credential;
        private readonly SecretClient _secretClient;

        public PerfStorageProvider(string vaultUri, TokenCredential credential)
        {
            _credential = credential ?? new DefaultAzureCredential();
            _secretClient = new SecretClient(new Uri(vaultUri), _credential);
        }

        public async Task<IPerfStorage> GetPerfStorageAsync()
        {
            var response = await _secretClient.GetSecretAsync(Constants.KeyVaultKey.StorageConnectionString);
            return new PerfStorage(response.Value.Value);
        }
    }
}
