using Plugin.Microsoft.Azure.SignalR.Benchmark;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Azure.SignalR.Benchmark.Tests
{
    public class TestSignalRUtils
    {
        [Fact]
        public void TestTryGetBatchMode()
        {
            var dic = new Dictionary<string, object>();
            SignalRUtils.TryGetBatchMode(
                dic,
                out string batchConfigMode,
                out int batchWaitMilliSeconds,
                out SignalREnums.BatchMode mode);
            Assert.Equal(batchConfigMode, SignalRConstants.DefaultBatchMode);
        }
    }
}
