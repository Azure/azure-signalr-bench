using Plugin.Microsoft.Azure.SignalR.Benchmark.Internals;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.Tests
{
    public class TestSimplePerfSend : TestPerfSend
    {
        public TestSimplePerfSend(ITestOutputHelper output) : base(output)
        {   
        }

        [Fact]
        public async Task TestSendToGroup()
        {
            _connections = 100;
            _sending = 100;
            _duration = 5000; // ms
            var group = 100;
            var arrvingRate = 5;
            var input = $@"
mode: simple
config:
  connectionString: Endpoint=https://dummy;AccessKey=dummy;Version=1.0; # Required
  singleStepDuration: {_duration}
  connections: {_connections}
  baseSending: {_sending}
  arrivingRate: {arrvingRate}
scenario:
  name: sendToGroup
  parameters:
    groupCount: {group}
";
            await _plugin.Start(input, _clients);
            CheckResult(GetBenchResult());
        }

        [Fact]
        public async Task TestSendToClients()
        {
            _connections = 100;
            _sending = 100;
            _duration = 5000; // ms
            var arrvingRate = 5;
            var input = $@"
mode: simple
config:
  connectionString: Endpoint=https://dummy;AccessKey=dummy;Version=1.0; # Required
  singleStepDuration: {_duration}
  connections: {_connections}
  baseSending: {_sending}
  arrivingRate: {arrvingRate}
scenario:
  name: sendToClient
";
            await _plugin.Start(input, _clients);
            CheckResult(GetBenchResult());
        }

        [Fact]
        public async Task TestBroadcast()
        {
            _connections = 10;
            _sending = 1;
            _duration = 5000; // ms
            var arrvingRate = 5;
            var input = $@"
mode: simple
config:
  connectionString: Endpoint=https://dummy;AccessKey=dummy;Version=1.0; # Required
  singleStepDuration: {_duration}
  connections: {_connections}
  baseSending: {_sending}
  step: {_sending}
  sendingSteps: 1
  arrivingRate: {arrvingRate}
scenario:
  name: broadcast
";
            await _plugin.Start(input, _clients);
            CheckResult(GetBenchResult());
        }

        [Fact]
        public async Task TestEcho()
        {
            _connections = 100;
            _sending = 100;
            _duration = 5000; // ms
            var arrvingRate = 10;
            var input = $@"
mode: simple
config:
  connectionString: Endpoint=https://dummy;AccessKey=dummy;Version=1.0; # Required
  singleStepDuration: {_duration}
  connections: {_connections}
  baseSending: {_sending}
  arrivingRate: {arrvingRate}
";
            await _plugin.Start(input, _clients);
            CheckResult(GetBenchResult());
        }
    }
}
