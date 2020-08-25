// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos.Table;

namespace Azure.SignalRBench.Storage
{
    public class TableAccessor<T> : ITableAccessor<T>
        where T : class, ITableEntity, new()
    {
        private readonly CloudTable _table;

        public TableAccessor(CloudTable table)
        {
            _table = table;
        }

        public TableQuery<T> Rows => new TableQuery<T>();

        public async Task<T> GetAsync(string partitionKey, string rowKey, CancellationToken cancellationToken)
        {
            var op = TableOperation.Retrieve<T>(partitionKey, rowKey);
            var result = await _table.ExecuteAsync(op, cancellationToken: cancellationToken);
            return (T)result.Result;
        }

        public async Task InsertAsync(T entity, CancellationToken cancellationToken)
        {
            var op = TableOperation.Insert(entity);
            var result = await _table.ExecuteAsync(op, cancellationToken: cancellationToken);
            entity.ETag = result.Etag;
        }

        public async Task UpdateAsync(T entity, CancellationToken cancellationToken)
        {
            var op = TableOperation.Replace(entity);
            var result = await _table.ExecuteAsync(op, cancellationToken: cancellationToken);
            entity.ETag = result.Etag;
        }

        public Task DeleteAsync(T entity, CancellationToken cancellationToken)
        {
            var op = TableOperation.Delete(entity);
            return _table.ExecuteAsync(op, cancellationToken: cancellationToken);
        }

        public async Task BatchInsertAsync(IReadOnlyList<T> entities, CancellationToken cancellationToken)
        {
            var batch = new TableBatchOperation();
            foreach (var entity in entities)
            {
                batch.Insert(entity);
            }
            var results = await _table.ExecuteBatchAsync(batch, cancellationToken: cancellationToken);
            for (int i = 0; i < entities.Count; i++)
            {
                entities[i].ETag = results[i].Etag;
            }
        }

        public async Task BatchUpdateAsync(IReadOnlyList<T> entities, CancellationToken cancellationToken)
        {
            var batch = new TableBatchOperation();
            foreach (var entity in entities)
            {
                batch.Replace(entity);
            }
            var results = await _table.ExecuteBatchAsync(batch, cancellationToken: cancellationToken);
            for (int i = 0; i < entities.Count; i++)
            {
                entities[i].ETag = results[i].Etag;
            }
        }

        public Task BatchDeleteAsync(IReadOnlyList<T> entities, CancellationToken cancellationToken)
        {
            var batch = new TableBatchOperation();
            foreach (var entity in entities)
            {
                batch.Delete(entity);
            }
            return _table.ExecuteBatchAsync(batch, cancellationToken: cancellationToken);
        }

        public async Task<T?> GetFirstOrDefaultAsync(TableQuery<T> query, CancellationToken cancellationToken)
        {
            var entities = await _table.ExecuteQuerySegmentedAsync(query.Take(1), null, cancellationToken: cancellationToken);
            return entities.FirstOrDefault();
        }

        public async IAsyncEnumerable<T> QueryAsync(TableQuery<T> query, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            TableContinuationToken? token = null;
            do
            {
                var entities = await _table.ExecuteQuerySegmentedAsync(query, token, cancellationToken: cancellationToken);
                token = entities.ContinuationToken;
                foreach (var item in entities)
                {
                    yield return item;
                }
            } while (token != null);
        }
    }
}
