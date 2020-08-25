// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.SignalRBench.Storage
{
    public interface IBlob
    {
        Task CreateIfNotExistedAsync();

        Task UploadAsync(string path, Stream source, IDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default);
        Task DownloadAsync(string path, Stream destination, CancellationToken cancellationToken = default);

        Task DeleteAsync(string path, CancellationToken cancellationToken = default);
        Task<bool> DeleteIfExistsAsync(string path, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default);

        Task<IDictionary<string, string>?> GetMetadataAsync(string path, CancellationToken cancellationToken = default);
        Task SetMetadataAsync(string path, IDictionary<string, string> metadata, CancellationToken cancellationToken = default);

        IAsyncEnumerable<string> List(string path, CancellationToken cancellationToken = default);
        Uri GetUri(string path);
    }
}