// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.SignalRBench.Storage
{
    public interface IFileShare
    {
        Task CreateIfNotExistsAsync(int quotaInGB);

        Task<bool> CopyAsync(string path, Uri uri, CancellationToken cancellationToken = default);

        Task<Stream> DownloadAsync(string path, CancellationToken cancellationToken = default);

        Task UploadAsync(string path, Stream source, CancellationToken cancellationToken = default);

        Task DeleteAsync(string path, CancellationToken cancellationToken = default);
        
        Task<bool> DeleteIfExistsAsync(string path, CancellationToken cancellationToken = default);
        
        Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default);

        Uri GetUri(string path);
    }
}