# General Benchmark framework

A general benchmark framework for evaluating the performance of general functionality. 

P.S. the framework is in [SignalRServiceBenchmarkPlugin_wanl branch](https://github.com/Azure/azure-signalr-bench/tree/SignalRServiceBenchmarkPlugin_wanl/SignalRServiceBenchmarkPlugin) for now.

## Architecture

The framework is a master-slave architecture, communicate through RPC.
```
       / slave 
master - slave
       \ slave
         ...
```
### Master
Master is responsible for loading and parsing benchmark configuration and allocating jobs to slaves to execute the benchmark.

### Slave
All slaves execute benchmark at the same time to evaluate the performance.

## Benchmark Plugin

Benchmark plugin can be anything only if it implements the interface in [plugins/base/PluginInterface/](https://github.com/Azure/azure-signalr-bench/tree/SignalRServiceBenchmarkPlugin_wanl/SignalRServiceBenchmarkPlugin/plugins/base/PluginInterface). A sample plugin is [plugins/Plugin.Microsoft.Azure.SignalR.Benchmark/](https://github.com/Azure/azure-signalr-bench/tree/SignalRServiceBenchmarkPlugin_wanl/SignalRServiceBenchmarkPlugin/plugins/Plugin.Microsoft.Azure.SignalR.Benchmark).

### Microsoft.Azure.SignalR.Benchmark

This plugin defines a set of typical performance scenarios and builds clients through SignalR client SDK to evaluate the performance of those scenarios.


#### Scenarios

The `Microsoft.Azure.SignalR.Benchmark` support several scenarios:

* Echo: send messages from client to server then back to the same client
* Broadcast: send messages to all clients on the same SignalR hub
* Peer to Peer: send messages to any client
* Send to Group: send messages to group
* Send to Group while group members frequently join and leave groups: some clients send messages to group while other clients join and leave group frequently 
* Concurrent connect to SignalR hub: client connect to Hub in a number at the same time
* Mix: this plugin and handle the mix of the above scenarios
* Record statistics: start/stop statistics collect, record anything interesting in benchmark

#### Benchmark configuration
``` yaml
# Define the plugin information for dynamiclly install plugin. Format: {Full class name}, {assembly name} 
ModuleName: Plugin.Microsoft.Azure.SignalR.Benchmark.SignalRBenchmarkPlugin, Plugin.Microsoft.Azure.SignalR.Benchmark

# Define the scenarios, can be executed at the same time
Types:
- P1
- P2

# Define the pipeline of the benchmark, the Type and Method is required. To support mix scenario, in one parallel step, multiple methods can be executed simultaneously
Pipeline:
...
-
  - Type: P1
    Method: CreateConnection
    Parameter.ConnectionTotal: 100
    Parameter.HubUrl: http://localhost:5050/signalrbench
    Parameter.Protocol: json
    Parameter.TransportType: WebSockets
  - Type: P2
    Method: CreateConnection
    Parameter.ConnectionTotal: 20
    Parameter.HubUrl: http://localhost:5000/signalrbench
    Parameter.Protocol: json
    Parameter.TransportType: WebSockets

...

# 6th parallel step
-
  - Type: P1
    Method: SendToClient
    Parameter.Duration: 20000
    Parameter.Interval: 1000
    Parameter.ConnectionTotal: 100
    Parameter.RemainderBegin: 0
    Parameter.RemainderEnd: 5
    Parameter.Modulo: 100
    Parameter.MessageSize: 20
  - Type: P2
    Method: Echo
    Parameter.Duration: 20000
    Parameter.Interval: 500
    Parameter.RemainderBegin: 0
    Parameter.RemainderEnd: 6
    Parameter.Modulo: 10
    Parameter.MessageSize: 40
...
```

Full configuration is in [BenchmarkConfigurationSample](https://github.com/Azure/azure-signalr-bench/blob/SignalRServiceBenchmarkPlugin_wanl/SignalRServiceBenchmarkPlugin/plugins/Plugin.Microsoft.Azure.SignalR.Benchmark/SignalRPlugin/BenchmarkConfigurationSample.yaml).

## Usage [TODO]

P.S. The commander is in [Commander branch](https://github.com/Azure/azure-signalr-bench/tree/Commander/SignalRServiceBenchmarkPlugin) for now.

In [utils/Commander/](https://github.com/Azure/azure-signalr-bench/tree/Commander/SignalRServiceBenchmarkPlugin/utils/Commander), the commander is the automation tool for the framework to controll the app server, master and slaves to start/stop via SSH, copy executables and benchmark configurations vis SCP.

To execute the `Microsoft.Azure.SignalR.Benchmark` plugin

```
todo: full commands here to execute the benchmark
```

