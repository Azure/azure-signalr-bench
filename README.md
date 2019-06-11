# Benchmark for Microsoft SignalR and Azure SignalR Service

This benchmark defines a set of typical performance scenarios, develops an Application Server with Azure SignalR SDK, and
builds clients through SignalR client SDK to evaluate the performance of those scenarios.

# Content

- Scripts: All scripts related to run the benchmark in a large scale, for example, 50 client VMs and 20 app server VMs.

- SignalRServiceBenchmarkPlugin: The benchmark source code folder which can be run on a single machine with 2 commands.

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

Configuration file "echo.yaml" is very simple:

For Azure SignalR Service, please specify "connectionString".
```yaml
mode: simple
config:
  connectionString: Endpoint=https://xxx;AccessKey=xxx
scenario:
  name: echo
```

If you want to evaluate SignalR performance, please specify the Hub URL of SignalR: "webAppTarget". Since this benchmark requires a predefined hub, please use [built-in app server](https://github.com/Azure/azure-signalr-bench/tree/master/SignalRServiceBenchmarkPlugin/utils/AppServer) as a reference.

```yaml
mode: simple
config:
  webAppTarget: http://localhost:5050/signalrbench
scenario:
  name: echo
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

You can see more options by Go to [master folder](https://github.com/Azure/azure-signalr-bench/tree/master/SignalRServiceBenchmarkPlugin/framework/master)

```
dotnet run -- --BenchmarkConfiguration ?
```

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
