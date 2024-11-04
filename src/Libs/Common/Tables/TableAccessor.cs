// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Data.Tables;
using Azure.SignalRBench.Coordinator.Entities;

namespace Azure.SignalRBench.Storage
{
    public class TableAccessor<T> : ITableAccessor<T>
        where T : class, IPerfTableEntity, new()
    {
        private readonly TableClient _table;

        public TableAccessor(string url,  TokenCredential tokenCredential, string tableName)
        {
            _table = new TableClient(new Uri(url), tableName, tokenCredential);
        }

        public async Task CreateIfNotExist()
        {
            await _table.CreateIfNotExistsAsync();
        }

        public async Task<T?> GetAsync(string partitionKey, string rowKey, CancellationToken cancellationToken)
        {
            var result= await _table.GetEntityIfExistsAsync<T>(partitionKey,rowKey,null,cancellationToken);

            return result.HasValue ? result.Value : null;
        }

        public async Task InsertAsync(T entity, CancellationToken cancellationToken)
        {
            entity.LastModified = DateTimeOffset.UtcNow;
            var result = await _table.AddEntityAsync(entity, cancellationToken: cancellationToken);
            entity.ETag = result.Headers.ETag!.Value;
        }

        public async Task UpdateAsync(T entity, CancellationToken cancellationToken)
        {
            entity.LastModified = DateTimeOffset.UtcNow;
            var result = await _table.UpdateEntityAsync(entity, entity.ETag, cancellationToken: cancellationToken);
            entity.ETag = result.Headers.ETag!.Value;
        }

        public Task DeleteAsync(T entity, CancellationToken cancellationToken)
        {
            return _table.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, entity.ETag, cancellationToken);
        }

        public async IAsyncEnumerable<T> QueryAsync(string query,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
           await foreach (var entity in _table.QueryAsync<T>(query, cancellationToken: cancellationToken))
           {
               yield return entity;
           }
        }
    }
}