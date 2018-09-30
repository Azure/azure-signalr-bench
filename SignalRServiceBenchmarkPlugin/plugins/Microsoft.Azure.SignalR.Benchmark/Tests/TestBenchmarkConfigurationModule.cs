using Microsoft.Azure.SignalR.Benchmark.DataModule;
using System;
using Xunit;

namespace Microsoft.Azure.SignalR.Benchmark.Tests
{
    public class TestBenchmarkConfigurationModule
    {
        [Fact]
        public void TestParse()
        {
            var input = @"---
ModuleName: moduleName
";
            var parser = new BenchmarkConfigurationModule();
            parser.Parse(input);
            Assert.True(parser.ModuleName == "moduleName", $"");
           
        }
    }
}
