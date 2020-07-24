// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Azure;
using Azure.SignalRBench.Storage;
using Xunit;

namespace Azure.SignalRBench.Tests.StorageTest
{
    public class BlobTest
    {
        [SkippableFact]
        public async Task TestBlobCrud()
        {
            var storage = new PerfStorage(Requirements.RequireStorage());
            var blob = await storage.GetBlobAsync("test", true);
            var content = new byte[10];
            var random = new Random();
            random.NextBytes(content);
            const string file = "a/b1";

            try
            {
                await blob.UploadAsync(file, new MemoryStream(content));

                var ms = new MemoryStream();
                await blob.DownloadAsync(file, ms);
                Assert.Equal(content, ms.ToArray());

                Assert.True(await blob.ExistsAsync(file));
                Assert.True(await blob.DeleteIfExistsAsync(file));
                Assert.False(await blob.DeleteIfExistsAsync(file));
                Assert.False(await blob.ExistsAsync(file));
            }
            finally
            {
                await blob.DeleteIfExistsAsync(file);
            }
        }

        [SkippableFact]
        public async Task TestBlobMetadata()
        {
            var storage = new PerfStorage(Requirements.RequireStorage());
            var blob = await storage.GetBlobAsync("test", true);
            var content = new byte[10];
            var random = new Random();

            const string file = "a/b2";
            var metadataExpected = new Dictionary<string, string>
            {
                ["Now"] = DateTime.Now.ToString(),
                ["random"] = random.Next().ToString(),
            };
            try
            {
                await blob.UploadAsync(file, new MemoryStream(content), metadataExpected);

                var metadata = await blob.GetMetadataAsync(file);
                DictionaryAssert.Equal(metadataExpected, metadata);

                var metadataUpdatedExpected = new Dictionary<string, string>
                {
                    ["Now"] = DateTime.Now.ToString(),
                    ["random"] = random.Next().ToString(),
                };
                await blob.SetMetadataAsync(file, metadataUpdatedExpected);
                var metadataUpdated = await blob.GetMetadataAsync(file);
                DictionaryAssert.Equal(metadataUpdatedExpected, metadataUpdated);

                await blob.DeleteAsync(file);
                Assert.False(await blob.ExistsAsync(file));
            }
            finally
            {
                await blob.DeleteIfExistsAsync(file);
            }
        }

        [SkippableFact]
        public async Task TestBlobThrow()
        {
            var storage = new PerfStorage(Requirements.RequireStorage());
            var blob = await storage.GetBlobAsync("test", true);
            const string file = "a/b3";

            await Assert.ThrowsAsync<RequestFailedException>(() => blob.DeleteAsync(file));
            await Assert.ThrowsAsync<RequestFailedException>(() => blob.DownloadAsync(file, new MemoryStream()));
        }

        [SkippableFact]
        public async Task TestBlobSas()
        {
            var storage = new PerfStorage(Requirements.RequireStorage());
            var blob = await storage.GetBlobAsync("test", true);
            const string file = "a/sas";
            var content = new byte[10];
            var random = new Random();
            random.NextBytes(content);

            try
            {
                await blob.UploadAsync(file, new MemoryStream(content));
                var uri = blob.GetUri(file);
                using var client = new HttpClient();
                var response = await client.GetAsync(uri);
                response.EnsureSuccessStatusCode();
                Assert.Equal(content, await response.Content.ReadAsByteArrayAsync());
            }
            finally
            {
                await blob.DeleteIfExistsAsync(file);
            }
        }

        [SkippableFact]
        public async Task TestBlobList()
        {
            var storage = new PerfStorage(Requirements.RequireStorage());
            var blob = await storage.GetBlobAsync("test", true);
            var allFiles = new[]
            {
                "b/c/d",
                "b/c/e",
                "b/d/e",
                "b/d/f",
                "c",
            };
            var content = new byte[10];
            var random = new Random();
            random.NextBytes(content);

            try
            {
                foreach (var file in allFiles)
                {
                    await blob.UploadAsync(file, new MemoryStream(content));
                }

                Assert.Equal(allFiles.OrderBy(x => x), (await blob.List("").ToListAsync()).OrderBy(x => x));
                Assert.Equal(allFiles.Where(x => x.StartsWith("b/")).OrderBy(x => x), (await blob.List("b/").ToListAsync()).OrderBy(x => x));
                Assert.Equal(allFiles.Where(x => x.StartsWith("b/c/")).OrderBy(x => x), (await blob.List("b/c/").ToListAsync()).OrderBy(x => x));
            }
            finally
            {
                foreach (var file in allFiles)
                {
                    await blob.DeleteIfExistsAsync(file);
                }
            }
        }
    }
}
