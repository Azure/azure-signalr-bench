# General Benchmark framework

A general benchmark framework for evaluating the performance of general functionality. 

P.S. the framework is in [SignalRServiceBenchmarkPlugin](https://github.com/Azure/azure-signalr-bench/tree/master/SignalRServiceBenchmarkPlugin) for now.

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

## Usage

### Usage For Simple 

To start with the benchmark, run the most simple benchmark scenario ```echo``` in your local machine with a few scripts. 

#### Linux

``` 
cd <PROJECT_ROOT>/Scripts
run_benchmark_simple.sh <PROJECT_ROOT> <AZURE_SIGNALR_CONNECTION_STRING>
```

#### Windows
``` 
cd <PROJECT_ROOT>/Scripts/
run_benchmark_simple.ps1 <PROJECT_ROOT> <BENCHMARK_CONFIGURATION> <AZURE_SIGNALR_CONNECTION_STRING>
```
A sample benchmark configuration is [BenchmarkConfigurationSample_oneline_run.yaml](https://github.com/Azure/azure-signalr-bench/blob/master/SignalRServiceBenchmarkPlugin/framework/master/BenchmarkConfigurationSample_oneline_run.yaml)

Simple graphic report will be save in ```<PROJECT_ROOT>/Scripts/report.svg```

### Usage For Common scenarios

* Echo: send messages from client to server then back to the same client
* Broadcast: send messages to all clients on the same SignalR hub
* Peer to Peer: send messages to any client
* Send to Group: send messages to group
* Send to Group while group members frequently join and leave groups: some clients send messages to group while other clients join and leave group frequently

#### Generate benchmark configuration

Use [generate.py](https://github.com/Azure/azure-signalr-bench/blob/master/SignalRServiceBenchmarkPlugin/plugins/Plugin.Microsoft.Azure.SignalR.Benchmark/Scripts/BenchmarkConfigurationGenerator/generate.py) to generate benchmark. Read more for [usage](https://github.com/Azure/azure-signalr-bench/blob/master/SignalRServiceBenchmarkPlugin/plugins/Plugin.Microsoft.Azure.SignalR.Benchmark/Scripts/BenchmarkConfigurationGenerator/readme.md) of generate.py.

##### Example to generate benchmark configuration

- Echo: ```python3 generate.py -u 5 -S echo -m -p json -t Websockets -U http://localhost:5050/signalrbench -so "counters.txt" -g tiny -d 5```
- Broadcast: ```python3 generate.py -u 5 -S broadcast -m -p json -t Websockets -U http://localhost:5050/signalrbench -so "counters.txt" -g tiny -d 5```
- Send To Client: ```python3 generate.py -u 5 -S sendToClient -m -p json -t Websockets -U http://localhost:5050/signalrbench -so "counters.txt" -g tiny -d 5```
- Send To Group: ```python3 generate.py -u 5 -S sendToGroup -m -p json -t Websockets -U http://localhost:5050/signalrbench -so "counters.txt" -g tiny -d 5```
- Frequently Join/Leave Group: ```python3 generate.py -u 5 -S frequentJoinLeaveGroup -m -p json -t Websockets -U http://localhost:5050/signalrbench -so "counters.txt" -g tiny -d 5```

#### Run Benchmark

Usually, one machine is unable to handle large number of connections and large number messages. 

The [commander](https://github.com/Azure/azure-signalr-bench/tree/master/SignalRServiceBenchmarkPlugin/utils/Commander) is the automation tool to run benchmark equally in several machines. For now, the automation tool only supports Linux.

You can use the automation tool to control app server, slaves and master to start, run benchmark and stop.

``` bash

# modify the variables here, you don't need to modify the scripts below
project_root=<PROJECT_ROOT>
username=<USER_NAME>
password=<PASSWORD>
benchmark_path=<BENCHMARK_PATH>
master_hostname=<MASTER_HOST>
slave_list=<SLAVE_HOST_A>:<RPC_PORT_A>,<SLAVE_HOST_B>:<RPC_PORT_B>
app_server_hostnames=<APP_SERVER_HOST_A>,<APP_SERVER_HOST_B>
remote_store_root=/home/$username/<REMOTE_FOLDER_TO_STORE_EXEC_ANDCONFIG>

# build app server
cd $project_root/SignalRServiceBenchmarkPlugin/utils/AppServer
dotnet build

# generate rpc protocol
cd $project_root/SignalRServiceBenchmarkPlugin/framework/rpc/
. ./util.sh
generate_proto

# build slave
cd $project_root/SignalRServiceBenchmarkPlugin/framework/slave
dotnet build

# build master
cd $project_root/SignalRServiceBenchmarkPlugin/framework/master
dotnet build


# run automation tool
cd $project_root/SignalRServiceBenchmarkPlugin/utils/Commander
dotnet run -- \
--SlaveList=$slave_list \
--MasterHostname=$master_hostname \
--AppServerHostnames=$app_server_hostnames \
--Username=$username \
--Password=$password \
--AppserverProject=$project_root/SignalRServiceBenchmarkPlugin/utils/AppServer \
--MasterProject=$project_root/SignalRServiceBenchmarkPlugin/framework/master/ \
--SlaveProject=$project_root/SignalRServiceBenchmarkPlugin/framework/master/ \
--BenchmarkConfiguration=$benchmark_path \
--AppserverTargetPath=$remote_store_root/appserver \
--MasterTargetPath=$remote_store_root/master \
--SlaveTargetPath=$remote_store_root/slave \
--BenchmarkConfigurationTargetPath=$remote_store_root/benchmark_config.yaml
```

### Complex scenarios

#### Combination Of Common Scenarios

The Microsoft.Azure.SignalR.Benchmark not only support single scenario benchmark, but also support combination of common scenarios, which can simulate  real-life scenarios, each scenario outputs one seperate statistics after running benchmark.

[Benchmark configuration format](#### Benchmark configuration format) is described below.

All common scenarios are defined in [BenchmarkConfigurationStep.py](https://github.com/Azure/azure-signalr-bench/blob/master/SignalRServiceBenchmarkPlugin/plugins/Plugin.Microsoft.Azure.SignalR.Benchmark/Scripts/BenchmarkConfigurationGenerator/Util/BenchmarkConfigurationStep.py)

A sample script for combination of common scenarios is in [Mix.py](https://github.com/Azure/azure-signalr-bench/blob/master/SignalRServiceBenchmarkPlugin/plugins/Plugin.Microsoft.Azure.SignalR.Benchmark/Scripts/BenchmarkConfigurationGenerator/Mix.py). Mention that in list ```pipeline```, outer items are executed in order, while inner items are executed parallelly.

**Structure of pipeline**

```

[[sub-step11, sub-step12], [sub-step21, sub-step22]]
 (    s t e p  -   1    )  (    s t e p  -   2    ) 
 
```


#### Custom benchmark configuration 

If you have your specific scenario (For example first echo, then broadcast), you can either create a script to generate benchmark configuration. 

If your specific scenario is similiar to one of the following scenarios, you can modify scripts for common scenarios to support it.

- Echo: ```Echo.py```
- Broadcast: ```Broadcast.py```
- Send To Client: ```SendToClient.py```
- Send To Group: ```SendToGroup.py```
- Frequently Join/Leave Group: ```FrequentJoinLeaveGroup.py```

#### Benchmark configuration format

The benchmark configuration is a yaml file. It has three components: ```ModuleName```, ```Pipeline```, and ```Types```
- ModuleName(string): Plugin's full assembly name.
- Types(string list): Different scenarios' name, useful for combination of scenarios.
- Pipeline(2-dimension string list): Each item in outer list if a ```step``` consists of several ```sub steps```. One ```sub step``` defines an method for some scenario. ```Sub steps``` in the same ```step``` are executed parallelly, while the ```steps``` are executed in order.

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