// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Azure.SignalRBench.Storage
{
    public interface IPerfStorage
    {
        ValueTask<IBlob> GetBlobAsync(string container, bool ensureCreated = true);

        ValueTask<IQueue<T>> GetQueueAsync<T>(string name, bool ensureCreated = true);

        ValueTask<IFileShare> GetFileShareAsync(string name);

        // todo : table
    }
}