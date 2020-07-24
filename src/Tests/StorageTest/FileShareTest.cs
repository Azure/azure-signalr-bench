// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Azure.SignalRBench.Storage;
using Xunit;

namespace Azure.SignalRBench.Tests.StorageTest
{
    public class FileShareTest
    {
        [SkippableFact]
        public async Task TestFileShareCrud()
        {
            var storage = new PerfStorage(Requirements.RequireStorage());
            var fileShare = await storage.GetFileShareAsync(nameof(TestFileShareCrud).ToLower());
            await fileShare.CreateIfNotExistsAsync(1);
            var content = new byte[100];
            var random = new Random();
            random.NextBytes(content);
            const string file = "a/b.c";
            await fileShare.UploadAsync(file, new MemoryStream(content));
            Assert.True(await fileShare.ExistsAsync(file));

            using (var stream = await fileShare.DownloadAsync(file))
            {
                var array = new byte[1000];
                var count = await stream.ReadAsync(array);
                Assert.Equal(content.Length, count);
                Assert.Equal(content, array.Take(count).ToArray());
            }

            await fileShare.DeleteAsync(file);
            Assert.False(await fileShare.DeleteIfExistsAsync(file));
            Assert.False(await fileShare.ExistsAsync(file));
        }

        [SkippableFact]
        public async Task TestFileShareCopy()
        {
            var storage = new PerfStorage(Requirements.RequireStorage());
            var fileShare = await storage.GetFileShareAsync(nameof(TestFileShareCopy).ToLower());
            await fileShare.CreateIfNotExistsAsync(1);
            var content = new byte[100];
            var random = new Random();
            random.NextBytes(content);
            const string file1 = "a/source";
            const string file2 = "a/dest";
            try
            {
                await fileShare.UploadAsync(file1, new MemoryStream(content));
                Assert.True(await fileShare.ExistsAsync(file1));

                var uri = fileShare.GetUri(file1);
                Assert.NotNull(uri);
                await fileShare.CopyAsync(file2, uri);

                Assert.True(await fileShare.ExistsAsync(file2));

                using var stream = await fileShare.DownloadAsync(file2);
                var array = new byte[1000];
                var count = await stream.ReadAsync(array);
                Assert.Equal(content.Length, count);
                Assert.Equal(content, array.Take(count).ToArray());
            }
            finally
            {
                await fileShare.DeleteIfExistsAsync(file1);
                await fileShare.DeleteIfExistsAsync(file2);
            }
        }
    }
}
