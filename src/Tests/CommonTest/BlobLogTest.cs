using Azure.SignalRBench.Common;
using Azure.SignalRBench.Tests;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CommonTest
{
    public class BlobLogTest
    {
        [SkippableFact]
        public void TestLogToBlob()
        {
            var storage = Requirements.RequireStorage();
            var provider = new BlobLoggerProvider($"test/{nameof(TestLogToBlob)}", ".log", storage);
            var logger = provider.CreateLogger("test");
            for (int i = 0; i < 10; i++)
            {
                logger.LogInformation("test from {TestClass}.{MethodName}", nameof(BlobLogTest), nameof(TestLogToBlob));
            }
            provider.Dispose();
        }
    }
}
