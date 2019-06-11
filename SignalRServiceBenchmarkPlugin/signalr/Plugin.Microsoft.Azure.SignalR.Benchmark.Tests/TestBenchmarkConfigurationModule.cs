using Plugin.Microsoft.Azure.SignalR.Benchmark;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Plugins.Microsoft.Azure.SignalR.Benchmark.Tests
{
    public class TestBenchmarkConfigurationModule
    {
        private readonly ITestOutputHelper _output;

        public TestBenchmarkConfigurationModule(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestSimpleConfigurationSteps()
        {
            var connections = 20000;
            var baseSending = 1000;
            var step = 500;
            var input = $@"
mode: simple                                            # Required: 'simple|advance', default is 'simple'
kind: perf                                            # Optional: 'perf|longrun|resultparser', default is 'perf'
config:
  connectionString: Endpoint=https://xxxx;AccessKey=ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789;Version=1.0; # Required
  connections: {connections}                            # Optional, default is 1000
  arrivingRate: 100
  baseSending: {baseSending}
  step: {step}
  debug: true
scenario:
  name: restSendToUser
";
            var benchmark = new BenchmarkConfiguration(input);
            _output.WriteLine($"Pipe steps: {benchmark.Pipeline.Count}");
        }

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
    Method: Echo
    Parameter.Total: 1000
    Parameter.Idle: 200
  - Type: P2
    Method: Create
    Parameter.Total: 999
    Parameter.Idle: 200
# second step
- 
  - Type: P1
    Method: Echo
    Parameter.Total: 1000
    Parameter.Idle: 200
  - Type: P2
    Method: Create
    Parameter.Total: 2000
    Parameter.Idle: 333
";
            var benchmarkConfiguration = new BenchmarkConfiguration(input);

            // Basic information
            Assert.True(benchmarkConfiguration.Types[0] == "P1", $"type1 != 'P1', '{benchmarkConfiguration.Types[0]} instead'");
            Assert.True(benchmarkConfiguration.Types[1] == "P2", $"type2 != 'P2'");

            // Step 1
            Assert.True((string)benchmarkConfiguration.Pipeline[0][0].Parameters["Type"] == "P1", $"type != P1, {benchmarkConfiguration.Pipeline[0][0].Parameters["Type"]} instead in step 1");
            Assert.True((string)benchmarkConfiguration.Pipeline[0][0].Parameters["Method"] == "Echo", $"method != Echo in step 1");
            Assert.True((string)benchmarkConfiguration.Pipeline[0][1].Parameters["Type"] == "P2", $"type != P2 in step 1");
            Assert.True((string)benchmarkConfiguration.Pipeline[0][1].Parameters["Method"] == "Create", $"method != Create in step 1");
            Assert.True(Convert.ToInt32(benchmarkConfiguration.Pipeline[0][1].Parameters["Parameter.Total"]) == 999, $"total != 999 in step 1");

            // Step 2
            Assert.True((string)benchmarkConfiguration.Pipeline[1][0].Parameters["Type"] == "P1", $"type != P1 in step 2");
            Assert.True((string)benchmarkConfiguration.Pipeline[1][0].Parameters["Method"] == "Echo", $"method != Echo in step 2");
            Assert.True((string)benchmarkConfiguration.Pipeline[1][1].Parameters["Type"] == "P2", $"type != P2 in step 2");
            Assert.True((string)benchmarkConfiguration.Pipeline[1][1].Parameters["Method"] == "Create", $"method != Create in step 2");
            Assert.True(Convert.ToInt32(benchmarkConfiguration.Pipeline[1][1].Parameters["Parameter.Idle"]) == 333, $"Idle != 333 in step 2");
        }
    }
}
 