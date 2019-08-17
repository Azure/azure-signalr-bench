using Newtonsoft.Json;
using Plugin.Microsoft.Azure.SignalR.Benchmark.Internals;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.Tests
{
    public class TestPerfSend : RpcServerFixture
    {
        protected int _connections;
        protected int _sending;
        protected int _duration;

        public TestPerfSend(ITestOutputHelper output) : base(output)
        {
        }

        protected BenchResult GetBenchResult(string output = null)
        {
            var resultFile = output != null ? output : SimpleBenchmarkModel.DEFAULT_OUTPUT_PATH;
            if (File.Exists(resultFile))
            {
                var percentileList = SignalRConstants.PERCENTILE_LIST.Split(",")
                                                     .Select(ind => Convert.ToDouble(ind)).ToArray();
                var sb = new StringBuilder();
                StatisticsParser.Parse(
                    resultFile,
                    percentileList,
                    SignalRConstants.LATENCY_STEP,
                    SignalRConstants.LATENCY_MAX,
                    x => { sb.Append(x); },
                    true);
                var jsonResult = sb.ToString();
                _output.WriteLine(jsonResult);
                var benchResult = JsonConvert.DeserializeObject<BenchResult>(jsonResult);
                return benchResult;
            }
            return null;
        }

        protected void CheckResult(BenchResult result)
        {
            Assert.True(result != null);
            Assert.True(result.Connections == _connections);
            Assert.True(result.Items.Length > 0);
            Assert.True(result.Items[0].SendingStep == _sending);
            Assert.True(result.Items[0].Message.TotalSend > 0);
            Assert.True(result.Items[0].Message.TotalRecv > 0);
        }
    }
}
