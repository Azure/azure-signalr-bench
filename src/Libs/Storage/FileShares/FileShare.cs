// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Azure.Storage;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Azure.Storage.Sas;

namespace Azure.SignalRBench.Storage
{
    public class FileShare : IFileShare
    {
        private readonly string _connectionString;
        private readonly string _shareName;
        private readonly ShareClient _client;
        private readonly StorageSharedKeyCredential _credential;

        public FileShare(string connectionString, string shareName)
        {
            _connectionString = connectionString;
            _shareName = shareName;
            _client = new ShareClient(_connectionString, _shareName);
            _credential = new StorageSharedKeyCredential(
                _client.AccountName,
                ConnectionStringHelper.GetAccountKeyFromConnectionString(connectionString));
        }

        public Task CreateIfNotExistsAsync(int quotaInGB) =>
            new ShareClient(_connectionString, _shareName).CreateIfNotExistsAsync(quotaInGB: quotaInGB);

        public async Task<bool> CopyAsync(string path, Uri uri, CancellationToken cancellationToken = default)
        {
            var client = GetShareFileClient(path);
            var response = await client.StartCopyAsync(uri, cancellationToken: cancellationToken);
            return await CheckCopyResultAsync(client, response.Value.CopyId, cancellationToken);
        }

        public async Task<Stream> DownloadAsync(string path, CancellationToken cancellationToken = default)
        {
            var client = GetShareFileClient(path);
            var response = await client.DownloadAsync(cancellationToken: cancellationToken);
            return response.Value.Content;
        }

        public async Task UploadAsync(string path, Stream source, CancellationToken cancellationToken = default)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                var directoryClient = _client.GetDirectoryClient(directory);
                await directoryClient.CreateIfNotExistsAsync();
            }
            var client = GetShareFileClient(path);
            await client.CreateAsync(source.Length);
            await client.UploadAsync(source, cancellationToken: cancellationToken);
        }

        public Task DeleteAsync(string path, CancellationToken cancellationToken = default) =>
            GetShareFileClient(path).DeleteAsync(cancellationToken: cancellationToken);

        public async Task<bool> DeleteIfExistsAsync(string path, CancellationToken cancellationToken = default)
        {
            var response = await GetShareFileClient(path).DeleteIfExistsAsync(cancellationToken: cancellationToken);
            return response.Value;
        }

        public async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
        {
            var response = await GetShareFileClient(path).ExistsAsync(cancellationToken: cancellationToken);
            return response.Value;
        }

        public Uri GetUri(string path)
        {
            var uri = GetShareFileClient(path).Uri;
            var sasBuilder = new ShareSasBuilder
            {
                ShareName = _shareName,
                FilePath = path,
                Resource = "f",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1),
            };
            sasBuilder.SetPermissions(ShareFileSasPermissions.Read);
            return new UriBuilder(uri) { Query = sasBuilder.ToSasQueryParameters(_credential).ToString() }.Uri;
        }

        private ShareFileClient GetShareFileClient(string path) =>
            new ShareFileClient(_connectionString, _shareName, path);

        private static async Task<bool> CheckCopyResultAsync(ShareFileClient client, string copyId, CancellationToken cancellationToken)
        {
            foreach (var delay in DelayHelper.GetDelaySequence())
            {
                var response = await client.GetPropertiesAsync(cancellationToken: cancellationToken);
                if (response.Value.CopyId != copyId)
                {
                    return false;
                }
                switch (response.Value.CopyStatus)
                {
                    case CopyStatus.Pending:
                        await Task.Delay(delay);
                        break;
                    case CopyStatus.Success:
                        return true;
                    case CopyStatus.Aborted:
                    case CopyStatus.Failed:
                        return false;
                    default:
                        // todo : log unknown status.
                        return false;
                }
            }
            throw new InvalidOperationException("Never!");
        }
    }
}
