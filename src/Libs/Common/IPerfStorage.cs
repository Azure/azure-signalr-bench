// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.SignalRBench.Coordinator.Entities;

namespace Azure.SignalRBench.Storage
{
    public interface IPerfStorage
    {
        string QueueUrl { get; }
        string TableUrl { get; }
        string BlobUrl { get; }
        ValueTask<IQueue<T>> GetQueueAsync<T>(string name, bool ensureCreated = true);

        ValueTask<ITableAccessor<T>> GetTableAsync<T>(string name, bool ensureCreated = true)
            where T : class,IPerfTableEntity, new();
    }
}