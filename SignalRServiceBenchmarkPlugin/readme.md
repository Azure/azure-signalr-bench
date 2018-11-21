# General Benchmark framework

A general benchmark framework for evaluating the performance of general functionality. 

* [Overview](#Overview)
* [Supported Scenarios](#Scenarios)
* [Usage](#Usage)
  * [Quick Try](#QuickTry)
  * [Automation Tool](#Automation)
* [Benchmark Configuration](#BenchmarkConfiguration)
  * [Scripts For Generating Benchmark Configuration](#GenerateBenchmarkConfiguration)
## Benchmark Plugin

Benchmark plugin can be anything only if it implements the interface in [plugins/base/PluginInterface/](https://github.com/Azure/azure-signalr-bench/tree/master/SignalRServiceBenchmarkPlugin/plugins/base/PluginInterface). A sample plugin is [plugins/Plugin.Microsoft.Azure.SignalR.Benchmark/](https://github.com/Azure/azure-signalr-bench/tree/master/SignalRServiceBenchmarkPlugin/plugins/Plugin.Microsoft.Azure.SignalR.Benchmark).



### Microsoft.Azure.SignalR.Benchmark

Evaluating the performance of Azure SignalR Service is the initial requirement for this benchmark. Meanwhile, there are some users of Azure SignalR Service who also want to compare the performance of Azure-SignalR with other products, therefore, they want to understand how to develop a customized stress test. This benchmark can be taken as a reference. 

<a name="Overview"></a>

#### Overview
The methodology used by this benchmark is to find the maximum message sending with a critieria that 99% of messages' end to end latency is less than 1s. Let us take 'send to client' as an example. First, SignalR client connect to Auzre SignalR Service at the same speed. After all clients connected, for example, 1000 clients to SignalR service, those 1000 clients can send message to each other. For SendToClient, the benchmark firstly randomly selects a series of clients, for example, 500 as the target clients, which means there are 500 concurrent message sending to those clients every second. If 500 x 99% message's latency is less than 1 second after running a specified duration, the benchmark selects more targets to allow more batch message sending. While sending, the benchmark collect statistics of messages' latency and connections' status per second. As the concurrent message sending increases, there are two results: either all clients send message each other, or the critieria (99% messages' latency less than 1 second) cannot be met. The benchmark will stop. at the end of the benchmark, user can get benchmark statistics. See [Usage Section](#Usage) for more detail. 

The benchmark is designed to be [master-slave mode](https://github.com/Azure/azure-signalr-bench/wiki/Architecture). One master controls one/many slave(s). Master node assigns SignalR clients randomly to every slaves nodes, and assigns benchmark tasks to every slave node, and slave node is responsible to connect SignalR service, send message, collect message latency and report it to master node. In case user has a highly stress test, an [automation tool](#Automation) for controlling master node slave(s) node and app server node(s) is also included.

Since Azure SignalR requires developer or user to define a hub to receive message, this benchmark depends on a [customized App Server](https://github.com/Azure/azure-signalr-bench/tree/master/SignalRServiceBenchmarkPlugin/utils/AppServer) which is also included. 

Azure-SignalR support 3 transport types: Websockets, ServerSentEvents, and Longpolling, 2 kinds of protocols: json and messagepack, and many scenarios: echo, send to group, send to client, broadcast, serverless broadcast, serverless send to user. All of those cases are covered in this benchmark with different options and configurations.

Message size also impacts the latency, so the benchmark allows to specify message size.

<a name="Scenarios"></a>


#### Supported Scenarios

The `Microsoft.Azure.SignalR.Benchmark` support several typical scenarios (echo, broadcast, send-to-client, send-to-group, frequent-join/leave-group) which cover major features for Azure SignalR Service. 

All these scenarios can be treated as components, combine them together make a real-world scenario. For example, a common chat room scenario can be mix some/all of the echo, broadcast, send-to-client, send-to-group, frequent-join/leave-group together.

##### Scenario Basics

In each scenario, SignalR clients connect to Azure SignalR Service at the same speed. After all clients connected, core message sending process starts to send messages at the same message rate. This process may repeat several times with different number of clients sending messages while other doing nothing.

To evaluate performance, the number of clients sending messages is increasing by some amount. The process stop util the latency exceed some threshold or disconnected clients exceed some threshold. In between 2 adjacent processes, if some clients are disconnected but still in threshold, the clients will try to reconnect to Service to ensure the stress keeps the same in every process.

while the main part of the benchmark running, a statistics collector collect messages' latency and assigns then into 0-100 ms, 100-200 ms, ..., 900-1000 ms, > 1000 ms latency slots. The statistics also records the connection status and join/leave group numbers. The collector collects per second.  

For example, 1000 clients connect to Azure SignalR Service at speed 100 per second. After about 10 seconds, all clients connected to service. 200 clients then send 200 messages per second in 1 minutes. After that, 400 clients then send 400 messages per second in 1 minutes, ... The benchmark will stop if the percentage of messages' latencies that geater than 1 second is larger than 1%. 


> ##### Echo
> Choose part/all of the clients to send messages from client to server then back to the same client.

> ##### Broadcast
> Choose part/all of the clients to send messages to all clients on the same SignalR hub.

> ##### Send To Client: send messages to any client
> Choose part/all of the clients to send messages to some client via a ramdom connection ID.

> ##### Send to Group: send messages to group
> Divide the connections into several groups. Choose part/all of the clients to send messages to the group the client is in. Finally, leave groups.

> ##### Send to Group while group members frequently join and leave groups: some clients send messages to group while other clients join and leave group frequently
> Divide the connections into several groups. Choose part/all of the clients to send messages to the group the client is in, while other clients keep join/leave groups. Finally, leave groups.
 
##### Mix: this plugin and handle the mix of the above scenarios
Combine part/all of the scenarios to make real-world scenario.

<a name="Usage"></a>

## Usage

The benchmark is designed to be master-slave mode. One master controls one/many slave(s). Master node load [benchmark configurations](#BenchmarkConfiguration) assigns SignalR clients randomly to every slaves nodes, and assigns benchmark tasks to every slave node, and slave node is responsible to connect SignalR service, send message, collect message latency and report it to master node. 

[Quick Try](#Quick) describes how to run benchmark in local node, master/salve(s) and app server are in the same node. [Automation Tool For Multiple Nodes](#Automation) describes how to run benchmark with remote nodes (typically, on master node, several app server nodes and several app server node) to split stress to several nodes.

<a name="QuickTry"></a>

### Quick Try 

To start with the benchmark, run the most simple benchmark scenario ```echo``` in your local machine with a few scripts. 


``` 
# Linux
cd <PROJECT_ROOT>/Scripts
./run_benchmark_simple.sh BenchmarkConfigurationSample_oneline_run.yaml "<AZURE_SIGNALR_CONNECTION_STRING>"

# Windows
cd <PROJECT_ROOT>/Scripts/
run_benchmark_simple.ps1 (Resolve-Path BenchmarkConfigurationSample_oneline_run.yaml) "<AZURE_SIGNALR_CONNECTION_STRING>"
```
A sample benchmark configuration is [BenchmarkConfigurationSample_oneline_run.yaml](https://github.com/Azure/azure-signalr-bench/blob/master/Scripts/BenchmarkConfigurationSample_oneline_run.yaml). You can modify parameters in this file or input your own benchmark. To quickly generate typical scenarios, see [Generate Benchmark Configuration](# Scripts For Generating Benchmark Configuration) for more detail.

```x
cd <PROJECT_ROOT>/Scripts

# Linux
./run_benchmark_simple.sh <BENCHMARK_CONFIGURATION> "<AZURE_SIGNALR_CONNECTION_STRING>"

# Windows
run_benchmark_simple.ps1 <BENCHMARK_CONFIGURATION> "<AZURE_SIGNALR_CONNECTION_STRING>"

``` 


Simple graphic report will be save in ```<PROJECT_ROOT>/Scripts/report.svg```

<a name="Automation"></a>


### Automation Tool For Multiple Nodes

Usually, one node is unable to handle large number of connections and large number messages if the performance test is a highly stress one. A automation tool is provided to help to split stress to multiple slave nodes. The [commander](https://github.com/Azure/azure-signalr-bench/tree/master/SignalRServiceBenchmarkPlugin/utils/Commander) is the automation tool to run benchmark in several nodes. For now, the automation tool only supports Linux. The automation tool is responsible for controlling app server, slaves and master to start, run benchmark and stop.

[automation_tool.sh](https://github.com/Azure/azure-signalr-bench/blob/master/Scripts/automation_tool.sh) is the script to use the automation tool. You should provide the essential information in the variable section to execute the script.

``` bash
# variable section 
username=<USERNAME>
password=<PASSWORD>
benchmark_path=<BENCHMARK_CONFIGURATION_PATH>
master_hostname=<MASTER_HOSTNAME>
slave_list=<SLAVE_HOSTNAME_A>:<RPC_PORT_A>,<SLAVE_HOSTNAME_B>:<RPC_PORT_B>
app_server_hostnames=<APP_SERVER_HOSTNAME_A>,<APP_SERVER_HOSTNAME_B>
remote_store_root=<REMOTE_STORE_ROOT_FOR_EXE_AND_CONFIG>
azure_signalr_connection_string=<AZURE_SIGNALR_CONNECTION_STRING>
# end of variable section
```
Simple graphic report will be save in ```<PROJECT_ROOT>/Scripts/report.svg```

<a name="BenchmarkConfiguration"></a>
## Benchmark Configuration

Benchmark configuration defines pipeline to evalute perfomance.

<a name="GenerateBenchmarkConfiguration"></a>
### Scripts For Generating Benchmark Configuration

Use [generate.py](https://github.com/Azure/azure-signalr-bench/blob/master/SignalRServiceBenchmarkPlugin/plugins/Plugin.Microsoft.Azure.SignalR.Benchmark/Scripts/BenchmarkConfigurationGenerator/generate.py) to generate benchmark. Read more for [usage](https://github.com/Azure/azure-signalr-bench/blob/master/SignalRServiceBenchmarkPlugin/plugins/Plugin.Microsoft.Azure.SignalR.Benchmark/Scripts/BenchmarkConfigurationGenerator/readme.md) of generate.py.

##### Example to generate benchmark configuration

- Echo: ```python3 generate.py --unit 5 --scenario echo --url <HUB_URL>```
- Broadcast: ```python3 generate.py --unit 5 --scenario broadcast --url <HUB_URL>```
- Send To Client: ```python3 generate.py --unit 5 --scenario sendToClient --url <HUB_URL>```
- Send To Group: ```python3 generate.py --unit 5 --scenario sendToGroup --url <HUB_URL> --group_type tiny```
- Frequent Join/Leave Group: ```python3 generate.py --unit 5 --scenario frequentJoinLeaveGroup --url <HUB_URL> --group_type tiny```

Since the parameters for different units are usually different, the recommended parameters are saved in [settings.yaml](https://github.com/Azure/azure-signalr-bench/blob/master/SignalRServiceBenchmarkPlugin/plugins/Plugin.Microsoft.Azure.SignalR.Benchmark/Scripts/BenchmarkConfigurationGenerator/settings.yaml). You can modify the file to change the parameters to the benchmark configuartion. 

#### Custom benchmark configuration 

If you have special scenario (For example first echo, then broadcast), you can either create a script or modify the existing similar scripts to generate benchmark configuration. You can even write configuration directly. 

If your specific scenario is similar to one of the following scenarios, you can modify scripts for common scenarios to support it.

- Echo: [Echo.py](https://github.com/Azure/azure-signalr-bench/blob/master/SignalRServiceBenchmarkPlugin/plugins/Plugin.Microsoft.Azure.SignalR.Benchmark/Scripts/BenchmarkConfigurationGenerator/Echo.py)
- Broadcast: [Broadcast.py](https://github.com/Azure/azure-signalr-bench/blob/master/SignalRServiceBenchmarkPlugin/plugins/Plugin.Microsoft.Azure.SignalR.Benchmark/Scripts/BenchmarkConfigurationGenerator/Broadcast.py)
- Send To Client: [SendToClient.py](https://github.com/Azure/azure-signalr-bench/blob/master/SignalRServiceBenchmarkPlugin/plugins/Plugin.Microsoft.Azure.SignalR.Benchmark/Scripts/BenchmarkConfigurationGenerator/SendToClient.py)
- Send To Group: [SendToGroup.py](https://github.com/Azure/azure-signalr-bench/blob/master/SignalRServiceBenchmarkPlugin/plugins/Plugin.Microsoft.Azure.SignalR.Benchmark/Scripts/BenchmarkConfigurationGenerator/SendToGroup.py)
- Frequently Join/Leave Group: [FrequentJoinLeaveGroup.py](https://github.com/Azure/azure-signalr-bench/blob/master/SignalRServiceBenchmarkPlugin/plugins/Plugin.Microsoft.Azure.SignalR.Benchmark/Scripts/BenchmarkConfigurationGenerator/FrequentJoinLeaveGroup.py)


#### Combination Of primitive Scenarios

The Microsoft.Azure.SignalR.Benchmark not only support single scenario benchmark, but also support combination of primitive scenarios, which can simulate real-life scenarios, each scenario outputs one seperate statistics after running benchmark.

A primitive scenario focus on only one thing. For example, create/start/stop connections, echo/broadcast/send to group/frequently join and leave group, join/leave group and so on. All primitive scenarios are defined in [BenchmarkConfigurationStep.py](https://github.com/Azure/azure-signalr-bench/blob/master/SignalRServiceBenchmarkPlugin/plugins/Plugin.Microsoft.Azure.SignalR.Benchmark/Scripts/BenchmarkConfigurationGenerator/Util/BenchmarkConfigurationStep.py)


##### Script For Mix Scenario
A sample script for combining primitive scenarios is in [Mix.py](https://github.com/Azure/azure-signalr-bench/blob/master/SignalRServiceBenchmarkPlugin/plugins/Plugin.Microsoft.Azure.SignalR.Benchmark/Scripts/BenchmarkConfigurationGenerator/Mix.py). Mention that the list ```pipeline```, outer items are executed in order, while inner items are executed parallelly.

**Structure of pipeline**

```

[[sub-step11, sub-step12], [sub-step21, sub-step22]]
 (    s t e p  -   1    )  (    s t e p  -   2    ) 
 
```

#### Benchmark Configuration Format

The benchmark configuration is a yaml file. It has three components: ```ModuleName```, ```Pipeline```, and ```Types```
- ModuleName(string): Plugin's full assembly name.
- Types(string list): Different scenarios' name, useful for combination of scenarios.
- Pipeline(2-dimension string list): Each item in outer list if a ```step``` consists of several ```sub steps```. One ```sub step``` defines an method for some scenario. ```Sub steps``` in the same ```step``` are executed parallelly, while the ```steps``` are executed in order.

Although you can create benchmark configuration, but we recommend to use scripts to generate benchmark configuration.

``` yaml

# Mix scenarios of Echo and Send To Client

ModuleName: Plugin.Microsoft.Azure.SignalR.Benchmark.SignalRBenchmarkPlugin, Plugin.Microsoft.Azure.SignalR.Benchmark
Pipeline:
- - Method: InitStatisticsCollector
    Type: mix_echo
  - Method: InitStatisticsCollector
    Type: mix_send_to_client
- - Method: CollectStatistics
    Parameter.Interval: 1000
    Parameter.StatisticsOutputPath: ./counters_mix_echo.txt
    Type: mix_echo
  - Method: CollectStatistics
    Parameter.Interval: 1000
    Parameter.StatisticsOutputPath: ./counters_mix_send_to_client.txt
    Type: mix_send_to_client
- - Method: CreateConnection
    Parameter.ConnectionTotal: 16000
    Parameter.HubUrl: http://host1:5050/signalrbench
    Parameter.Protocol: json
    Parameter.TransportType: Websockets
    Type: mix_echo
  - Method: CreateConnection
    Parameter.ConnectionTotal: 4000
    Parameter.HubUrl: http://host2:5050/signalrbench
    Parameter.Protocol: json
    Parameter.TransportType: Websockets
    Type: mix_send_to_client
- - Method: StartConnection
    Parameter.ConcurrentConnetion: 100
    Type: mix_echo
  - Method: StartConnection
    Parameter.ConcurrentConnetion: 100
    Type: mix_send_to_client
- - Method: RegisterCallbackRecordLatency
    Type: mix_echo
  - Method: RegisterCallbackRecordLatency
    Type: mix_send_to_client
- - Method: CollectConnectionId
    Type: mix_send_to_client
###################################################
# this is a step
-
  # this is a sub step
  - Method: Echo
    Parameter.Duration: 60000
    Parameter.Interval: 1000
    Parameter.MessageSize: 1
    Parameter.Modulo: 160
    Parameter.RemainderBegin: 0
    Parameter.RemainderEnd: 40
    Type: mix_echo
  # this is another sub step, these two sub steps will be executed at the same time
  - Method: SendToClient
    Parameter.ConnectionTotal: 4000
    Parameter.Duration: 60000
    Parameter.Interval: 1000
    Parameter.MessageSize: 1
    Parameter.Modulo: 40
    Parameter.RemainderBegin: 0
    Parameter.RemainderEnd: 10
    Type: mix_send_to_client
###################################################
- - Method: Wait
    Parameter.Duration: 5000
    Type: mix_echo
  - Method: Wait
    Parameter.Duration: 5000
    Type: mix_send_to_client
- - Method: StopCollector
    Type: mix_echo
  - Method: StopCollector
    Type: mix_send_to_client
- - Method: StopConnection
    Type: mix_echo
  - Method: StopConnection
    Type: mix_send_to_client
- - Method: DisposeConnection
    Type: mix_echo
  - Method: DisposeConnection
    Type: mix_send_to_client
Types:
- mix_echo
- mix_send_to_client


```