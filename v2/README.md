# RPC-based benchmark framework

- [Why do we need to develop this benchmark framework](#Why)
- [Overview](#Overview)
- [How to use it](#How)
- [Typical use case](#TypicalCase)

<a name="Why"></a>
## Why do we need to develop this benchmark framework

Evaluating the performance of Azure-SignalR is the initial requirement for this benchmark. Meanwhile, there are some users of Azure-SignalR who also want to compare the performance of Azure-SignalR with other products, therefore, they want to understand how to develop a customized stress test. This benchmark can be taken as a reference.

<a name="Overview"></a>
## Overview

The methodology used by this benchmark is to find the maximum message sending with a critieria that 99% of messages' end to end latency is less than 1s. Let us take 'send to client' as an example. After establishing, for example, 1000 connections to SignalR service, those 1000 clients can send message to each other. For SendToClient, the benchmark firstly randomly selects a series of clients, for example, 500 as the target clients, which means there are 500 concurrent message sending to those clients every second. If 500 x 99% message's latency is less than 1 second after running a specified duration, the benchmark selects more targets to allow more batch message sending. As the concurrent message sending increases, there are two results: either all clients send message each other, or the critieria (99% messages' latency less than 1 second) cannot be met. The benchmark will stop.

The benchmark is designed to be master-slave mode. One master controls many slaves. Master node assigned tasks to every slave node, and slave node is responsible to connect SignalR service, send message, collect message latency and report it to master node.

Since Azure SignalR requires developer or user to define a hub to receive message, this benchmark depends on a customized App Server which is also included.

Azure-SignalR support 3 transport types: Websockets, ServerSentEvents, and Longpolling, 2 kinds of protocols: json and messagepack, and many scenarios: echo, send to group, send to client, broadcast, serverless broadcast, serverless send to user. All of those cases are covered in this benchmark with different options and configurations.

Message size also impacts the latency, so the benchmark allows to specify message size.

<a name="How"></a>
## How to use it

This benchmark essentially includes a bunch of SignalR clients, which connects to the specified App Server, and send message to clients, groups, or broadcast to all. For serverless mode, the clients directly connect to Azure-SignalR service. In order to cover all of those scenarios, this benchmark includes different kinds of XXXOp.

### Server mode

Setup 3 VMs for App server, master node, and slave node. You can use the same VM for all of them for a quick try.

- Launch the App server

Go to `AppServer` folder, set connection string and start it.

`dotnet user-secrets set Azure:SignalR:ConnectionString "Endpoint=https://XXX;AccessKey=YYY;Version=1.0"`

`dotnet run`

Let us assume the app server hub URL is "http://appserver:5050/signalrbench"

- Launch the slave node under Rpc/Bench.Server folder

Specify the listening port. Here uses 7000 as an example. Let us assume the slave node IP is "10.0.0.10"

`dotnet run -- --rpcPort 7000`

- Launch the master node under Rpc/Bench.Client folder

Here we take Websockets transport, json protocol, with 2k message for echo scenario as an example. The concurrent connection build rate is 100 connections every second. The pipeline here specifies the test running step: first create 1000 client objects, secondly start connections, then sending 500 message every second, after running for 300 seconds, sending more 500 + 500 messages per second for another 300 seconds. The latency distribution is saved to counters.txt

`dotnet run -- --rpcPort 7000 --duration 300 --connections 1000 --serverUrl "http://appserver:5050/signalrbench" -t Websockets -p json -s echo --messageSize 2k --concurrentConnection 100 --slaveList "10.0.0.10" --pipeLine "createConn;startConn;up500;scenario;up500;scenario;stopConn;disposeConn" -o counters.txt`

### Serverless mode

Let us take send to user as an example.

Setup 2 VMs for master node and slave node. You can use the same VM for them.

Launch the slave node

`dotnet run --rpcPort 7000`

Launch the master node:

`dotnet run -- --rpcPort 7000 --duration 300 --connections 1000 -t Websockets -p json -s RestSendToUser --messageSize 2k --concurrentConnection 100 --slaveList "10.0.0.10" --pipeLine "createRestClientConn;startRestClientConn;up500;scenario;up500;scenario;stopConn;disposeConn" -o counters.txt`

<a name="TypicalCase"></a>
## Typical case

### Group

The benchmark automatically assigns the connections to be groups. If connection number is larger than group number, every group has more than 1 connection. Otherwise, if group number is larger than connection number, it means 1 connection joins more than 1 group.

For sending message to group, we want to know how many groups and how big groups can we support. This scenario can be benchmarked by specifying a pipeline for group scenario and group numbers.

The steps are as follows:

Launch the App server and slave node, those two steps are the same as above.

Launch master node to support 1000 connections, with 100 groups and every group has 10 connections. In every second, there are 500 connections sending message to its own group. The tool evaluates the latency of every message.

`dotnet run -- --rpcPort 7000 --duration 300 --connections 1000 -t Websockets -p json -s SendGroup --messageSize 2k --concurrentConnection 100 --slaveList "10.0.0.10" --pipeLine "createConn;startConn;joinGroup;up500;scenario;leaveGroup;stopConn;disposeConn" --groupNum 100 -o counters.txt`


