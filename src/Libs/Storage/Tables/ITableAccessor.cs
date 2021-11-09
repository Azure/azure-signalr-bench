// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos.Table;

namespace Azure.SignalRBench.Storage
{
    public interface ITableAccessor<T> where T : class, ITableEntity, new()
    {
        IQueryable<T> Rows { get; }

        Task<T> GetAsync(string partitionKey, string rowKey, CancellationToken cancellationToken = default);

        Task InsertAsync(T entity, CancellationToken cancellationToken = default);

        Task UpdateAsync(T entity, CancellationToken cancellationToken = default);

        Task DeleteAsync(T entity, CancellationToken cancellationToken = default);

        Task BatchInsertAsync(IReadOnlyList<T> entities, CancellationToken cancellationToken = default);

        Task BatchUpdateAsync(IReadOnlyList<T> entities, CancellationToken cancellationToken = default);

        Task BatchDeleteAsync(IReadOnlyList<T> entities, CancellationToken cancellationToken = default);

        Task<T?> GetFirstOrDefaultAsync(IQueryable<T> query, CancellationToken cancellationToken = default);

        IAsyncEnumerable<T> QueryAsync(IQueryable<T> query, CancellationToken cancellationToken = default);
        
        List<T> QueryAsync(TableQuery<T> query, int n=int.MaxValue, CancellationToken cancellationToken = default);
    }
}