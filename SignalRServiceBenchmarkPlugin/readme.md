# General Benchmark framework

A general benchmark framework for evaluating the performance of general functionality. 

P.S. the framework is in [SignalRServiceBenchmarkPlugin_wanl branch](https://github.com/Azure/azure-signalr-bench/tree/SignalRServiceBenchmarkPlugin_wanl/SignalRServiceBenchmarkPlugin) for now.

## Benchmark Plugin

Benchmark plugin can be anything only if it implements the interface in [plugins/base/PluginInterface/](https://github.com/Azure/azure-signalr-bench/tree/SignalRServiceBenchmarkPlugin_wanl/SignalRServiceBenchmarkPlugin/plugins/base/PluginInterface). A sample plugin is [plugins/Plugin.Microsoft.Azure.SignalR.Benchmark/](https://github.com/Azure/azure-signalr-bench/tree/SignalRServiceBenchmarkPlugin_wanl/SignalRServiceBenchmarkPlugin/plugins/Plugin.Microsoft.Azure.SignalR.Benchmark).



### Microsoft.Azure.SignalR.Benchmark

This plugin defines a set of typical performance scenarios and builds clients through SignalR client SDK to evaluate the performance of those scenarios.


#### Supported Scenarios

The `Microsoft.Azure.SignalR.Benchmark` support several scenarios:

* Echo: send messages from client to server then back to the same client
* Broadcast: send messages to all clients on the same SignalR hub
* Peer to Peer: send messages to any client
* Send to Group: send messages to group
* Send to Group while group members frequently join and leave groups: some clients send messages to group while other clients join and leave group frequently 
* Concurrent connect to SignalR hub: client connect to Hub in a number at the same time
* Mix: this plugin and handle the mix of the above scenarios
* Record statistics: start/stop statistics collect, record anything interesting in benchmark

## Usage For Microsoft.Azure.SignalR.Benchmark

The [commander](https://github.com/Azure/azure-signalr-bench/tree/master/SignalRServiceBenchmarkPlugin/utils/Commander) is the automation tool for the framework to control the app server, master and slaves to start/stop via SSH, copy executables and benchmark configurations vis SCP.

### One line to run the simplest Microsoft.Azure.SignalR.Benchmark





