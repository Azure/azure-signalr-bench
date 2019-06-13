using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.Tests
{
    public class TestAdvancePerfSend : TestPerfSend
    {
        public TestAdvancePerfSend(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task TestBroadcast()
        {
            _connections = 10;
            _sending = 1;
            _duration = 5000; // ms
            var arrvingRate = 5;
            var messageSize = 2048;
            var benchOut = "broadcast_output.txt";
            var dummyUrl = "Endpoint=https://dummy;AccessKey=dummy;Version=1.0;";

            var input = $@"
ModuleName: Plugin.Microsoft.Azure.SignalR.Benchmark.SignalRBenchmarkPlugin, Plugin.Microsoft.Azure.SignalR.Benchmark
Pipeline:
- - Method: InitStatisticsCollector
    Parameter.LatencyMax: 1000
    Parameter.LatencyStep: 100
    Type: broadcast
- - Method: CreateConnection
    Parameter.ConnectionTotal: {_connections}
    Parameter.HubUrl: http://localhost:5050/signalrbench
    Parameter.Protocol: json
    Parameter.TransportType: Websockets
    Type: broadcast
- - Method: CollectStatistics
    Parameter.Interval: 1000
    Parameter.StatisticsOutputPath: {benchOut}
    Type: broadcast
- - Method: StartConnection
    Parameter.BatchMode: HighPress
    Parameter.BatchWait: 1000
    Parameter.ConcurrentConnection: {arrvingRate}
    Type: broadcast
- - Method: Wait
    Parameter.Duration: 1000
    Type: broadcast
- - Method: RegisterCallbackRecordLatency
    Type: broadcast
- - Method: Reconnect
    Parameter.BatchMode: HighPress
    Parameter.BatchWait: 1000
    Parameter.ConcurrentConnection: 100
    Parameter.ConnectionTotal: {_connections}
    Parameter.HubUrl: {dummyUrl}
    Parameter.Protocol: json
    Parameter.TransportType: Websockets
    Type: broadcast
- - Method: Broadcast
    Parameter.Duration: {_duration}
    Parameter.Interval: 1000
    Parameter.MessageSize: {messageSize}
    Parameter.Modulo: {_connections}
    Parameter.RemainderBegin: 0
    Parameter.RemainderEnd: 1
    Type: broadcast
- - Method: Wait
    Parameter.Duration: 1000
    Type: broadcast
- - Method: StopCollector
    Type: broadcast
- - Method: StopConnection
    Type: broadcast
- - Method: DisposeConnection
    Type: broadcast
Types:
- broadcast
";
            await _plugin.Start(input, _clients);
            CheckResult(GetBenchResult(benchOut));
        }

        [Fact]
        public async Task TestEcho()
        {
            _connections = 100;
            _sending = 100;
            _duration = 5000; // ms
            var arrvingRate = 10;
            var messageSize = 2048;
            var benchOut = "bench.out";
            var dummyUrl = "Endpoint=https://dummy;AccessKey=dummy;Version=1.0;";
            var input = $@"
ModuleName: Plugin.Microsoft.Azure.SignalR.Benchmark.SignalRBenchmarkPlugin, Plugin.Microsoft.Azure.SignalR.Benchmark
Pipeline:
- - Method: InitStatisticsCollector
    Parameter.LatencyMax: 1000
    Parameter.LatencyStep: 100
    Type: echo
- - Method: CreateConnection
    Parameter.ConnectionTotal: {_connections}
    Parameter.HubUrl: {dummyUrl}
    Parameter.Protocol: json
    Parameter.TransportType: Websockets
    Type: echo
- - Method: CollectStatistics
    Parameter.Interval: 1000
    Parameter.StatisticsOutputPath: {benchOut}
    Type: echo
- - Method: StartConnection
    Parameter.BatchMode: LowPress
    Parameter.BatchWait: 1000
    Parameter.ConcurrentConnection: {arrvingRate}
    Type: echo
- - Method: Wait
    Parameter.Duration: 1000
    Type: echo
- - Method: RegisterCallbackRecordLatency
    Type: echo
- - Method: Reconnect
    Parameter.BatchMode: LowPress
    Parameter.BatchWait: 1000
    Parameter.ConcurrentConnection: {arrvingRate}
    Parameter.ConnectionTotal: {_connections}
    Parameter.HubUrl: {dummyUrl}
    Parameter.Protocol: json
    Parameter.TransportType: Websockets
    Type: echo
- - Method: Echo
    Parameter.Duration: {_duration}
    Parameter.Interval: 1000
    Parameter.MessageSize: {messageSize}
    Parameter.Modulo: {_connections}
    Parameter.RemainderBegin: 0
    Parameter.RemainderEnd: {_connections}
    Type: echo
- - Method: Wait
    Parameter.Duration: 1000
    Type: echo
- - Method: StopCollector
    Type: echo
- - Method: StopConnection
    Type: echo
- - Method: DisposeConnection
    Type: echo
Types:
- echo
";
            await _plugin.Start(input, _clients);
            CheckResult(GetBenchResult(benchOut));
        }
    }
}
