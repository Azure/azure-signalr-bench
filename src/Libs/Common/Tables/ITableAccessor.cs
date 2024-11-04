// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;


namespace Azure.SignalRBench.Storage
{
    public interface ITableAccessor<T> where T : class, ITableEntity, new()
    {
        Task CreateIfNotExist();
        Task<T?> GetAsync(string partitionKey, string rowKey, CancellationToken cancellationToken = default);

        Task InsertAsync(T entity, CancellationToken cancellationToken = default);

        Task UpdateAsync(T entity, CancellationToken cancellationToken = default);

        Task DeleteAsync(T entity, CancellationToken cancellationToken = default);

        IAsyncEnumerable<T> QueryAsync(string filter, CancellationToken cancellationToken = default);
    }
}