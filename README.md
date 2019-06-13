# Benchmark for Microsoft SignalR and Azure SignalR Service

This benchmark defines some typical performance scenarios, develops an Application Server with Azure SignalR SDK, and
builds clients through SignalR client SDK to evaluate the performance of those scenarios.

This benchmark targets to help you evaluate the throughput and latency. It can be used for both SignalR and Azure SignalR Service.

# Content

- **Scripts**: All scripts related to run the benchmark in a large scale, for example, 50 client VMs and 20 app server VMs. If you plan to setup a large scale performance test on Azure, those scripts are for your reference.

- **SignalRServiceBenchmarkPlugin**: The benchmark source code folder which can be run on a single machine with 2 commands. If you want to get a quick start, or check the benchmark source code for further development, please go to this folder.

# Quick start

Take 1000 connections for `Echo` performance test as an example.

Go to [slave folder](https://github.com/Azure/azure-signalr-bench/tree/master/SignalRServiceBenchmarkPlugin/framework/slave)
```
dotnet run
```

Go to [master folder](https://github.com/Azure/azure-signalr-bench/tree/master/SignalRServiceBenchmarkPlugin/framework/master)

```
dotnet run -- --BenchmarkConfiguration echo.yaml
```
Configuration file "echo.yaml" is very simple. There are two required values: `mode` and `webAppTarget`. `mode` is always `simple`. `webAppTarget` is the SignalR Hub URL. Default scenario is "echo" even though you did not specify it.

```yaml
mode: simple
config:
  webAppTarget: http://localhost:5050/signalrbench
```

If you have a Azure SignalR Service connection string, but does not want to setup the app server, you can specify connection string instead. The benchmark will launch an internal app server for you. It is recommended for serious performance test.

If you want to setup your own app server for this benchmark, please use [built-in app server](https://github.com/Azure/azure-signalr-bench/tree/master/SignalRServiceBenchmarkPlugin/utils/AppServer) as a reference because the benchmark tool requires a predefined Hub.

Use internal app server, please specify `connectionString` instead.
```yaml
mode: simple
config:
  connectionString: Endpoint=https://xxx;AccessKey=xxx
```

The output is like:
```
-----------
  1000 connections established in 8s
-----------
 Connections/sendingStep: 1000/500 in 246s
 Messages: requests: 244.88MB, responses: 244.88MB
   Requests/sec: 484.17
   Responses/sec: 484.17
   Write throughput: 995.46KB
   Read throughput: 995.46KB
 Latency:
  50%: < 100 ms
  90%: < 100 ms
  95%: < 100 ms
  99%: < 100 ms
-----------
 Connections/sendingStep: 1000/1000 in 245s
 Messages: requests: 489.71MB, responses: 489.71MB
   Requests/sec: 972.18
   Responses/sec: 972.18
   Write throughput: 2.00MB
   Read throughput: 2.00MB
 Latency:
  50%: < 100 ms
  90%: < 100 ms
  95%: < 100 ms
  99%: < 100 ms
```

# More options

You can see more options by running the following command in [master folder](https://github.com/Azure/azure-signalr-bench/tree/master/SignalRServiceBenchmarkPlugin/framework/master)

```
dotnet run -- --BenchmarkConfiguration ?
```

The benchmark supports many scenarios including echo, broadcast, send to connections, and send to groups. Those scenarios apply on both SignalR and Azure SignalR Service. In addition, it supports Azure SignalR specific scenarios, for example, scenarios on serverless mode: send to user, send to group, and broadcast through REST API.

## Typical scenarios

The following configuration examples are for both SignalR and Azure SignalR Service. Before running those test, please start the app server and replace the `webAppTarget` with your Hub URL.

- broadcast: broadcast a message to 100 clients every second
```yaml
mode: simple
config:
  webAppTarget: http://localhost:5050/signalrbench
  connections: 100
  baseSending: 1
  step: 1
  sendingSteps: 1
scenario:
  name: broadcast
```
- send to connections: send messages to 1000 clients
```yaml
mode: simple
config:
  webAppTarget: http://localhost:5050/signalrbench
scenario:
  name: sendToClient
```
- send to groups: create 500 groups for 1000 connections, which means every group has 2 connections. The benchmark sends message to every group.
```yaml
mode: simple
config:
  webAppTarget: http://localhost:5050/signalrbench
  connections: 1000
  baseSending: 500
  step: 100
scenario:
  name: sendToGroup
  parameters:
    groupCount: 500
```

## Azure SignalR Service specific scenarios.

Please replace the `connectionString` with your own one.

- restBroadcast
```yaml
mode: simple
config:
  connectionString: Endpoint=https://xxx;AccessKey=xxx
  connections: 100
  baseSending: 1
  step: 1
  sendingSteps: 1
scenario:
  name: restBroadcast
```
- restSendToUser
```yaml
mode: simple
config:
  connectionString: Endpoint=https://xxx;AccessKey=xxx
scenario:
  name: restSendToUser
```
- restSendToGroup
```yaml
mode: simple
config:
  connectionString: Endpoint=https://xxx;AccessKey=xxx
  connections: 1000
  baseSending: 500
  step: 100
scenario:
  name: restSendToGroup
  parameters:
    groupCount: 500
```

## Default settings

Default connection transport type is `Websockets`, protocol is `json`, message size is 2048. Feel free to change those if you want to test more configurations.

# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
