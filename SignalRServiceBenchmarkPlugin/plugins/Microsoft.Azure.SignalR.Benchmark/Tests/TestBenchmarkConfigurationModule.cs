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
ModuleName: myModuleName

Types:
- P1
- P2

Pipeline:
 # first step
-
  - Type: P1
    Method: echo
    Parameter:
      Total: 1000
      Idle: 200
  - Type: P2
    Method: create
    Parameter:
      Total: 2000
      Idle: 200
# second step
- 
  - Type: P1
    Method: Echo
    Parameter:
      Total: 1000
      Idle: 200
  - Type: P2
    Method: create
    Parameter:
      Total: 2000
      Idle: 200
";
            var parser = new BenchmarkConfigurationModule();
            parser.Parse(input);
            Assert.True(parser.ModuleName == "myModuleName", $"moduleName != 'myModuleName'");
            Assert.True(parser.Types[0] == "P1", $"type1 != 'P1', '{parser.Types[0]} instead'");
            Assert.True(parser.Types[1] == "P2", $"type2 != 'P2'");
            Assert.True(parser.Pipeline[0][0].Type == "P1", $"type != P1");
            Assert.True(parser.Pipeline[0][0].Method == "echo", $"method != Echo");
        }
    }
}
 