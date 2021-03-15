// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos.Table;

namespace Azure.SignalRBench.Storage
{
    public class PerfStorage : IPerfStorage
    {
        private readonly string _saConnectionString;
        private readonly string _cdbConnectionString;

        public PerfStorage(string saConnectionString,string cdbConnectionString=null)
        {
            _saConnectionString = saConnectionString;
            _cdbConnectionString = cdbConnectionString;
        }

        public async ValueTask<IBlob> GetBlobAsync(string container, bool ensureCreated)
        {
            var result = new Blob(_saConnectionString, container);
            if (ensureCreated)
            {
                await result.CreateIfNotExistedAsync();
            }
            return result;
        }

        public async ValueTask<IQueue<T>> GetQueueAsync<T>(string name, bool ensureCreated)
        {
            var result = new Queue<T>(_saConnectionString, name);
            if (ensureCreated)
            {
                await result.CreateIfNotExistedAsync();
            }
            return result;
        }

        public ValueTask<IFileShare> GetFileShareAsync(string name)
        {
            var result = new FileShare(_saConnectionString, name);
            return new ValueTask<IFileShare>(result);
        }

        public async ValueTask<ITableAccessor<T>> GetTableAsync<T>(string name)
            where T : class, ITableEntity, new()
        {
            var storageAccount = CloudStorageAccount.Parse(_cdbConnectionString);
            var client = storageAccount.CreateCloudTableClient();
            var cloudTable = client.GetTableReference(name);
            await cloudTable.CreateIfNotExistsAsync();
            return new TableAccessor<T>(cloudTable);
        }
    }
}
