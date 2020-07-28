// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;

namespace Azure.SignalRBench.Storage
{
    public class Blob : IBlob
    {
        private readonly BlobServiceClient _serviceClient;
        private readonly BlobContainerClient _client;
        private readonly string _container;
        private readonly StorageSharedKeyCredential _credential;

        public Blob(string connectionString, string container)
        {
            _container = container;
            _serviceClient = new BlobServiceClient(connectionString);
            _client = _serviceClient.GetBlobContainerClient(container);
            _credential = ConnectionStringHelper.GetCredential(connectionString);
        }

        public Task CreateIfNotExistedAsync() =>
            _client.CreateIfNotExistsAsync();

        public Task DownloadAsync(string path, Stream destination, CancellationToken cancellationToken = default)
        {
            var client = _client.GetBlobClient(path);
            return client.DownloadToAsync(destination, cancellationToken);
        }

        public Task UploadAsync(string path, Stream source, IDictionary<string, string> metadata = null, CancellationToken cancellationToken = default)
        {
            var client = _client.GetBlockBlobClient(path);
            return client.UploadAsync(source, metadata: metadata, cancellationToken: cancellationToken);
        }

        public async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
        {
            var client = _client.GetBlobClient(path);
            var response = await client.ExistsAsync(cancellationToken);
            return response.Value;
        }

        public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
        {
            var client = _client.GetBlobClient(path);
            return client.DeleteAsync(cancellationToken: cancellationToken);
        }

        public async Task<bool> DeleteIfExistsAsync(string path, CancellationToken cancellationToken = default)
        {
            var client = _client.GetBlobClient(path);
            var response = await client.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            return response.Value;
        }

        public async Task<IDictionary<string, string>> GetMetadataAsync(string path, CancellationToken cancellationToken = default)
        {
            var client = _client.GetBlobClient(path);
            var response = await client.GetPropertiesAsync(cancellationToken: cancellationToken);
            return response.Value?.Metadata;
        }

        public Task SetMetadataAsync(string path, IDictionary<string, string> metadata, CancellationToken cancellationToken = default)
        {
            var client = _client.GetBlobClient(path);
            return client.SetMetadataAsync(metadata, cancellationToken: cancellationToken);
        }

        public IAsyncEnumerable<string> List(string path, CancellationToken cancellationToken = default)
        {
            return _client.GetBlobsByHierarchyAsync(prefix: path, cancellationToken: cancellationToken)
                .Select(x => x.Blob.Name);
        }

        public Uri GetUri(string path)
        {
            var client = _client.GetBlobClient(path);
            var uri = client.Uri;
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _container,
                BlobName = path,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1),
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);
            var sas = sasBuilder.ToSasQueryParameters(_credential);
            return new UriBuilder(uri) { Query = sas.ToString() }.Uri;
        }
    }
}
