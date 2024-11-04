// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Azure.Identity;
using Azure.SignalRBench.Coordinator.Entities;

namespace Azure.SignalRBench.Storage
{
    public class PerfStorage : IPerfStorage
    {
        private readonly string _queueUrl;
        private readonly string _cdbUrl;
        private readonly string _blobUrl;
        private readonly string _msiClientId;
        
        public string QueueUrl => _queueUrl;
        public string TableUrl => _cdbUrl;
        public string BlobUrl => _blobUrl;
        

        public PerfStorage(string queueUrl, string cdbUrl, string blobUrl, string msiClientId)
        {
            _queueUrl = queueUrl;
            _cdbUrl = cdbUrl;
            _blobUrl = blobUrl;
            _msiClientId = msiClientId;
        }
        
        public async ValueTask<IQueue<T>> GetQueueAsync<T>(string name, bool ensureCreated)
        {
            var result = new Queue<T>(_queueUrl, new DefaultAzureCredential(
                new DefaultAzureCredentialOptions { ManagedIdentityClientId =  _msiClientId}
                ), name);
            if (ensureCreated)
            {
                await result.CreateIfNotExistedAsync();
            }
            return result;
        }

        public async ValueTask<ITableAccessor<T>> GetTableAsync<T>(string name, bool ensureCreated)
            where T :class,IPerfTableEntity, new()
        {
            var tableAccessor = new TableAccessor<T>(_cdbUrl, new DefaultAzureCredential(
                new DefaultAzureCredentialOptions()
                {
                    ManagedIdentityClientId = _msiClientId
                }
                ), name);
            if (ensureCreated)
                await tableAccessor.CreateIfNotExist();
            return tableAccessor;
        }
    }
}