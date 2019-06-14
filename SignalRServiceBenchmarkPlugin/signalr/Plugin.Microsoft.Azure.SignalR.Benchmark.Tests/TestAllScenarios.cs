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

        #region simple perf
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
        #endregion

        #region simple longrun
        [Fact]
        public async Task TestEchoLongrun()
        {
            _connections = 100;
            _sending = 100;
            _duration = 5000; // ms
            var arrvingRate = 10;
            var input = $@"
mode: simple
kind: longrun
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

        [Fact]
        public async Task TestSendToGroupLongrun()
        {
            _connections = 100;
            _sending = 100;
            _duration = 5000; // ms
            var group = 100;
            var arrvingRate = 5;
            var input = $@"
mode: simple
king: longrun
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
        public async Task TestSendToClientsLongrun()
        {
            _connections = 100;
            _sending = 100;
            _duration = 5000; // ms
            var arrvingRate = 5;
            var input = $@"
mode: simple
kind: longrun
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
        public async Task TestBroadcastLongrun()
        {
            _connections = 10;
            _sending = 1;
            _duration = 5000; // ms
            var arrvingRate = 5;
            var input = $@"
mode: simple
kind: longrun
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
        #endregion

        #region advanced longrun
        [Fact]
        public async Task TestAdvancedEchoLongrun()
        {
            _connections = 100;
            _sending = 100;
            _duration = 5000; // ms
            var arrvingRate = 5;
            var output = "echo_longrun.txt";
            var dummyUrl = "Endpoint=https://dummy;AccessKey=dummy;Version=1.0;";
            var input = $@"
ModuleName: Plugin.Microsoft.Azure.SignalR.Benchmark.SignalRBenchmarkPlugin, Plugin.Microsoft.Azure.SignalR.Benchmark
Pipeline:
- - Method: CreateConnection
    Parameter.ConnectionTotal: {_connections}
    Parameter.HubUrl: {dummyUrl}
    Parameter.Protocol: json
    Parameter.TransportType: Websockets
    Type: echo
- - Method: InitConnectionStatisticsCollector
    Parameter.LatencyMax: 1000
    Parameter.LatencyStep: 100
    Type: echo
- - Method: CollectConnectionStatistics
    Parameter.Interval: 1000
    Parameter.PercentileList: 0.5,0.9,0.95,0.99
    Parameter.StatisticsOutputPath: {output}
    Type: echo
- - Method: RegisterCallbackOnConnected
    Type: echo
- - Method: RegisterCallbackRecordLatency
    Type: echo
- - Method: StartConnection
    Parameter.BatchMode: HighPress
    Parameter.BatchWait: 1000
    Parameter.ConcurrentConnection: {arrvingRate}
    Type: echo
- - Method: RepairConnections
    Parameter.ActionAfterConnect: None
    Type: echo
- - Method: Echo
    Parameter.Duration: {_duration}
    Parameter.Interval: 1000
    Parameter.MessageSize: 2048
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
            CheckResult(GetBenchResult());
        }

        [Fact]
        public async Task TestAdvancedSendToGroupLongrun()
        {
            _connections = 100;
            _sending = 100;
            _duration = 5000; // ms
            var groupCount = 100;
            var arrvingRate = 5;
            var output = "sendtogroup_longrun.txt";
            var dummyUrl = "Endpoint=https://dummy;AccessKey=dummy;Version=1.0;";
            var input = $@"
ModuleName: Plugin.Microsoft.Azure.SignalR.Benchmark.SignalRBenchmarkPlugin, Plugin.Microsoft.Azure.SignalR.Benchmark
Pipeline:
- - Method: CreateConnection
    Parameter.ConnectionTotal: {_connections}
    Parameter.HubUrl: {dummyUrl}
    Parameter.Protocol: json
    Parameter.TransportType: Websockets
    Type: sendToGroup
- - Method: InitConnectionStatisticsCollector
    Parameter.LatencyMax: 1000
    Parameter.LatencyStep: 100
    Type: sendToGroup
- - Method: CollectConnectionStatistics
    Parameter.Interval: 1000
    Parameter.PercentileList: 0.5,0.9,0.95,0.99
    Parameter.StatisticsOutputPath: {output}
    Type: sendToGroup
- - Method: RegisterCallbackOnConnected
    Type: sendToGroup
- - Method: RegisterCallbackRecordLatency
    Type: sendToGroup
- - Method: StartConnection
    Parameter.BatchMode: HighPress
    Parameter.BatchWait: 1000
    Parameter.ConcurrentConnection: {arrvingRate}
    Type: sendToGroup
- - Method: JoinGroup
    Parameter.ConnectionTotal: {_connections}
    Parameter.GroupCount: {groupCount}
    Type: sendToGroup
- - Method: Wait
    Parameter.Duration: 1000
    Type: sendToGroup
- - Method: RepairConnections
    Parameter.ActionAfterConnect: JoinToGroup
    Type: sendToGroup
- - Method: SendToGroup
    Parameter.ConnectionTotal: {_connections}
    Parameter.Duration: {_duration}
    Parameter.GroupCount: {groupCount}
    Parameter.Interval: 1000
    Parameter.MessageSize: 2048
    Parameter.Mode: Connection
    Parameter.Modulo: {_connections}
    Parameter.RemainderBegin: 0
    Parameter.RemainderEnd: {_connections}
    Type: sendToGroup
- - Method: Wait
    Parameter.Duration: 1000
    Type: sendToGroup
- - Method: LeaveGroup
    Parameter.ConnectionTotal: {_connections}
    Parameter.GroupCount: {groupCount}
    Type: sendToGroup
- - Method: StopCollector
    Type: sendToGroup
- - Method: StopConnection
    Type: sendToGroup
- - Method: DisposeConnection
    Type: sendToGroup
Types:
- sendToGroup
";
            await _plugin.Start(input, _clients);
            CheckResult(GetBenchResult());
        }
        #endregion

        #region advanced perf
        [Fact]
        public async Task TestAdvanceSendToClient()
        {
            _connections = 100;
            _sending = 100;
            _duration = 5000; // ms
            var arrvingRate = 5;
            var output = "sendtoclient_result.txt";
            var dummyUrl = "Endpoint=https://dummy;AccessKey=dummy;Version=1.0;";
            var input = $@"
ModuleName: Plugin.Microsoft.Azure.SignalR.Benchmark.SignalRBenchmarkPlugin, Plugin.Microsoft.Azure.SignalR.Benchmark
Pipeline:
- - Method: InitStatisticsCollector
    Parameter.LatencyMax: 1000
    Parameter.LatencyStep: 100
    Type: sendToClient
- - Method: CreateConnection
    Parameter.ConnectionTotal: {_connections}
    Parameter.HubUrl: {dummyUrl}
    Parameter.Protocol: json
    Parameter.TransportType: Websockets
    Type: sendToClient
- - Method: CollectStatistics
    Parameter.Interval: 1000
    Parameter.StatisticsOutputPath: {output}
    Type: sendToClient
- - Method: StartConnection
    Parameter.BatchMode: HighPress
    Parameter.BatchWait: 1000
    Parameter.ConcurrentConnection: {arrvingRate}
    Type: sendToClient
- - Method: Wait
    Parameter.Duration: 1000
    Type: sendToClient
- - Method: RegisterCallbackRecordLatency
    Type: sendToClient
- - Method: Reconnect
    Parameter.BatchMode: HighPress
    Parameter.BatchWait: 1000
    Parameter.ConcurrentConnection: {arrvingRate}
    Parameter.ConnectionTotal: {_connections}
    Parameter.HubUrl: {dummyUrl}
    Parameter.Protocol: json
    Parameter.TransportType: Websockets
    Type: sendToClient
- - Method: CollectConnectionId
    Type: sendToClient
- - Method: Wait
    Parameter.Duration: 1000
    Type: sendToClient
- - Method: Reconnect
    Parameter.BatchMode: HighPress
    Parameter.BatchWait: 1000
    Parameter.ConcurrentConnection: {arrvingRate}
    Parameter.ConnectionTotal: {_connections}
    Parameter.HubUrl: {dummyUrl}
    Parameter.Protocol: json
    Parameter.TransportType: Websockets
    Type: sendToClient
- - Method: Wait
    Parameter.Duration: 1000
    Type: sendToClient
- - Method: CollectConnectionId
    Type: sendToClient
- - Method: SendToClient
    Parameter.ConnectionTotal: {_connections}
    Parameter.Duration: {_duration}
    Parameter.Interval: 1000
    Parameter.MessageSize: 2048
    Parameter.Modulo: {_connections}
    Parameter.RemainderBegin: 0
    Parameter.RemainderEnd: {_connections}
    Type: sendToClient
- - Method: Wait
    Parameter.Duration: 1000
    Type: sendToClient
- - Method: StopCollector
    Type: sendToClient
- - Method: StopConnection
    Type: sendToClient
- - Method: DisposeConnection
    Type: sendToClient
Types:
- sendToClient
";
            await _plugin.Start(input, _clients);
            CheckResult(GetBenchResult(output));
        }

        [Fact]
        public async Task TestAdvanceSendToGroup()
        {
            _connections = 100;
            _sending = 100;
            _duration = 5000; // ms
            var group = 100;
            var arrvingRate = 5;
            var output = "sendtogroup_result.txt";
            var dummyUrl = "Endpoint=https://dummy;AccessKey=dummy;Version=1.0;";
            var input = $@"
ModuleName: Plugin.Microsoft.Azure.SignalR.Benchmark.SignalRBenchmarkPlugin, Plugin.Microsoft.Azure.SignalR.Benchmark
Pipeline:
- - Method: InitStatisticsCollector
    Parameter.LatencyMax: 1000
    Parameter.LatencyStep: 100
    Type: sendToGroup
- - Method: CreateConnection
    Parameter.ConnectionTotal: {_connections}
    Parameter.HubUrl: {dummyUrl}
    Parameter.Protocol: json
    Parameter.TransportType: Websockets
    Type: sendToGroup
- - Method: CollectStatistics
    Parameter.Interval: 1000
    Parameter.StatisticsOutputPath: {output}
    Type: sendToGroup
- - Method: StartConnection
    Parameter.BatchMode: HighPress
    Parameter.BatchWait: 1000
    Parameter.ConcurrentConnection: {arrvingRate}
    Type: sendToGroup
- - Method: Wait
    Parameter.Duration: 1000
    Type: sendToGroup
- - Method: RegisterCallbackRecordLatency
    Type: sendToGroup
- - Method: Reconnect
    Parameter.BatchMode: HighPress
    Parameter.BatchWait: 1000
    Parameter.ConcurrentConnection: {arrvingRate}
    Parameter.ConnectionTotal: {_connections}
    Parameter.HubUrl: {dummyUrl}
    Parameter.Protocol: json
    Parameter.TransportType: Websockets
    Type: sendToGroup
- - Method: JoinGroup
    Parameter.ConnectionTotal: {_connections}
    Parameter.GroupCount: {group}
    Type: sendToGroup
- - Method: Wait
    Parameter.Duration: 1000
    Type: sendToGroup
- - Method: SendToGroup
    Parameter.ConnectionTotal: {_connections}
    Parameter.Duration: {_duration}
    Parameter.GroupCount: {group}
    Parameter.Interval: 1000
    Parameter.MessageSize: 2048
    Parameter.Mode: Connection
    Parameter.Modulo: {_connections}
    Parameter.RemainderBegin: 0
    Parameter.RemainderEnd: {_connections}
    Type: sendToGroup
- - Method: Wait
    Parameter.Duration: 1000
    Type: sendToGroup
- - Method: LeaveGroup
    Parameter.ConnectionTotal: {_connections}
    Parameter.GroupCount: {group}
    Type: sendToGroup
- - Method: StopCollector
    Type: sendToGroup
- - Method: StopConnection
    Type: sendToGroup
- - Method: DisposeConnection
    Type: sendToGroup
Types:
- sendToGroup
";
            await _plugin.Start(input, _clients);
            CheckResult(GetBenchResult(output));
        }

        [Fact]
        public async Task TestAdvanceBroadcast()
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
    Parameter.HubUrl: {dummyUrl}
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
    Parameter.ConcurrentConnection: {arrvingRate}
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
        public async Task TestAdvanceEcho()
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

        #endregion
    }
}
