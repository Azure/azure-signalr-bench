using Plugin.Microsoft.Azure.SignalR.Benchmark;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.SignalR.Benchmark.Tests
{
    public class TestSignalRUtils
    {
        // write something to console:
        //   _output.WriteLine($"Token{i}: {tok}");
        //   dotnet test --logger:"console;verbosity=detailed"
        private readonly ITestOutputHelper _output;

        public TestSignalRUtils(ITestOutputHelper output)
        {
            _output = output;
        }

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
