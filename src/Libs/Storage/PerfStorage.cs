// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Azure.SignalRBench.Storage
{
    public class PerfStorage : IPerfStorage
    {
        private readonly string _connectionString;

        public PerfStorage(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async ValueTask<IBlob> GetBlobAsync(string container, bool ensureCreated)
        {
            var result = new Blob(_connectionString, container);
            if (ensureCreated)
            {
                await result.CreateIfNotExistedAsync();
            }
            return result;
        }

        public async ValueTask<IQueue<T>> GetQueueAsync<T>(string name, bool ensureCreated)
        {
            var result = new Queue<T>(_connectionString, name);
            if (ensureCreated)
            {
                await result.CreateIfNotExistedAsync();
            }
            return result;
        }

        public ValueTask<IFileShare> GetFileShareAsync(string name)
        {
            var result = new FileShare(_connectionString, name);
            return new ValueTask<IFileShare>(result);
        }
    }
}
