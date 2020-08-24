using Azure.SignalRBench.Common;
using Azure.SignalRBench.Storage;
using Azure.SignalRBench.Tests;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CommonTest
{
    public class BlobLogTest
    {
        [SkippableFact]
        public async Task TestLogToBlob()
        {
            var storage = Requirements.RequireStorage();
            var ps = new PerfStorage(storage);
            var blob = await ps.GetBlobAsync("logs", true);
            try
            {
                var provider = new BlobLoggerProvider($"test/{nameof(TestLogToBlob)}", ".log", storage);
                var logger = provider.CreateLogger("test");
                var threadId = Thread.CurrentThread.ManagedThreadId;
                var dt1 = DateTime.UtcNow;
                for (int i = 0; i < 10; i++)
                {
                    logger.LogInformation("test from {TestClass}.{MethodName}", nameof(BlobLogTest), nameof(TestLogToBlob));
                }
                var dt2 = DateTime.UtcNow;
                provider.Dispose();
                var paths = await blob.List("test/").ToListAsync();
                Assert.Single(paths);
                using var ms = new MemoryStream();
                await blob.DownloadAsync(paths[0], ms);
                ms.Seek(0, SeekOrigin.Begin);
                using var sr = new StreamReader(ms);
                var obj = JsonConvert.DeserializeObject<JObject>(sr.ReadLine());
                var dt = obj["EventTime"].ToObject<DateTime>();
                Assert.True(dt1 <= dt && dt <= dt2);
                Assert.Equal(0, obj["EventId"].ToObject<int>());
                Assert.Null(obj["EventName"].ToObject<string>());
                Assert.Equal(threadId, obj["ThreadId"].ToObject<int>());
                Assert.Equal("test", obj["Logger"].ToObject<string>());
                Assert.Equal("test from BlobLogTest.TestLogToBlob", obj["Text"].ToObject<string>());
                Assert.Null(obj["Exception"].ToObject<string>());
                Assert.Equal("BlobLogTest", obj["TestClass"].ToObject<string>());
                Assert.Equal("TestLogToBlob", obj["MethodName"].ToObject<string>());
            }
            finally
            {
                var paths = await blob.List("test/").ToListAsync();
                foreach (var path in paths)
                {
                    await blob.DeleteIfExistsAsync(path);
                }
            }
        }
    }
}
