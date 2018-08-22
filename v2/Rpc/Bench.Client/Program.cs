// Copyright 2015 gRPC authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Bench.Common;
using Bench.Common.Config;
using CommandLine;
using Grpc.Core;
using Newtonsoft.Json.Linq;

namespace Bench.RpcMaster
{
    class Program
    {
        private static JObject _counters;
        private static string _jobResultFile = "./jobResult.txt";
        // protect the file writing
        private static readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1);

        public static async Task Main(string[] args)
        {
            // parse args
            var argsOption = ParseArgs(args);

            // save pid
            SavePid(argsOption.PidFile);

            var slaveList = ParseSlaveListStr(argsOption.SlaveList);

            // generate rpc channels
            var channels = CreateRpcChannels(slaveList, argsOption.RpcPort, Util.isDebug(argsOption.Debug));

            try
            {
                if (argsOption.Clear == "true")
                {
                    if (File.Exists(_jobResultFile))
                    {
                        File.Delete(_jobResultFile);
                    }
                }
                else
                {
                    if (File.Exists(_jobResultFile))
                    {
                        CheckLastJobResults(_jobResultFile, argsOption.Retry, argsOption.Connections,
                            argsOption.ServiceType, argsOption.TransportType, argsOption.HubProtocal, argsOption.Scenario);
                    }
                }

                // create rpc clients
                var clients = CreateRpcClients(channels);

                // check rpc connections
                WaitRpcConnectSuccess(clients);

                // load job config
                var jobConfig = new JobConfig(argsOption);

                // call salves to load job config
                ClientsLoadJobConfig(clients, slaveList, argsOption);

                // collect counters
                StartCollectCounters(clients, argsOption.OutputCounterFile);

                // process jobs for each step
                await ProcessPipeline(clients, argsOption.PipeLine, slaveList,
                    argsOption.Connections, argsOption.ServiceType, argsOption.TransportType, argsOption.HubProtocal, argsOption.Scenario, argsOption.MessageSize,
                    argsOption.groupNum, argsOption.groupOverlap, argsOption.ServerUrl.Split(";").ToList().Count, argsOption.sendToFixedClient);
            }
            catch (Exception ex)
            {
                Util.Log($"Exception from RPC master: {ex}");
                SaveJobResult(_jobResultFile, null, argsOption.Connections, argsOption.ServiceType, argsOption.TransportType, argsOption.HubProtocal, argsOption.Scenario);
                throw;
            }
            SaveJobResult(_jobResultFile, _counters, argsOption.Connections, argsOption.ServiceType, argsOption.TransportType, argsOption.HubProtocal, argsOption.Scenario);

            WaitChannelsShutDown(channels);
            Console.WriteLine("Exit client...");
        }

        private static List<string> CollectConnectionIds(List<RpcService.RpcServiceClient> clients)
        {
            var connectionIds = new List<string>();
            clients.ForEach(client =>
            {
                var connectionIdsPerClient = client.GetConnectionIds(new Empty());
                connectionIds.AddRange(connectionIdsPerClient.List);
            });
            return connectionIds;
        }

        private static List<string> LeftShiftConnectionIdsOnEachClient(List<RpcService.RpcServiceClient> clients, int serverCount)
        {
            var connectionIds = new List<string>();
            clients.ForEach(client =>
            {
                var connectionIdsPerClient = client.GetConnectionIds(new Empty());
                var connectionIdsMoved = new List<string>(connectionIdsPerClient.List);
                connectionIdsMoved.CircleLeftShift();
                var connectionCount = connectionIdsMoved.Count;
                // The corner case: 1st connection and last connection connect to the same server
                // This case happens, for example, 3 connections: c0, c1, c2, 2 servers: s0, s1
                //    Connections to 2 servers: connection0(c0-s0), connection1(c1-s1), connection2(c2-s0)
                //    After left shift, the target is:  connection1, connection2, connection0
                //    which means message sent: connection0 -> connection1, connection1 -> connection2, connection2 -> connection0
                //    you see, connection2 and connection0 belong to the same server: s0.
                //    As a result, the message will not go to Redis.
                //    We force to change connection2 to send message to another connection1
                if ((connectionCount - 1) % serverCount == 0)
                {
                    connectionIdsMoved[connectionCount - 1] = connectionIdsMoved[connectionIdsMoved.Count <= 1 ? 0 : 1]; // one slave VM may have only one connection
                }
                connectionIds.AddRange(connectionIdsMoved);
            });
            return connectionIds;
        }

        private static List<string> GenerateGroupNameList(int connCnt, int groupNum, int overlap)
        {
            var groupNameList = Enumerable.Repeat("", connCnt).ToList();
            for (var j = 0; j < overlap; j++)
            {
                for (var i = 0; i < groupNameList.Count; i++)
                {
                    if (groupNameList[i].Length > 0) groupNameList[i] += ";";
                    groupNameList[i] += $"gp{(i + j) % groupNum}";
                }
            }
            groupNameList.Shuffle();
            return groupNameList;
        }

        private static void SaveConfig(string path, int connection, string serviceType, string transportType, string protocol, string scenario)
        {
            var jobj = new JObject
            { { "connection", connection }, { "serviceType", serviceType }, { "transportType", transportType }, { "protocol", protocol }, { "scenario", scenario }
            };

            string onelineRecord = Regex.Replace(jobj.ToString(), @"\s+", "");
            onelineRecord = Regex.Replace(onelineRecord, @"\t|\n|\r", "");
            onelineRecord += Environment.NewLine;

            Util.SaveContentToFile(path, onelineRecord, false);
        }

        private static void SaveToFile(string path, JObject jobj)
        {
            string onelineRecord = Regex.Replace(jobj.ToString(), @"\s+", "");
            onelineRecord = Regex.Replace(onelineRecord, @"\t|\n|\r", "");
            onelineRecord += Environment.NewLine;

            Util.SaveContentToFile(path, onelineRecord, true);
        }

        private static double GetSuccessPercentage(JObject counters, string scenario, int connection)
        {
            var sent = (int) counters["message:sent"];
            var notSent = (int) counters["message:notSentFromClient"];
            var total = sent + notSent;
            var received = (int) counters["message:received"];
            var percentage = 0.0;
            if (scenario.Contains("broadcast"))
            {
                percentage = (double) received / (total * connection);
            }
            else if (scenario.Contains("echo"))
            {
                percentage = (double) received / (total);
            }
            else if (scenario.Contains("mix"))
            {
                percentage = 1.0; // todo
            }
            else if (scenario.Contains("group"))
            {
                percentage = 1.0; // todo
            }

            return percentage;
        }

        private static void SaveJobResult(string path, JObject counters, int connection, string serviceType, string transportType, string protocol, string scenario)
        {
            // fail for sure
            if (counters == null)
            {
                var resFail = new JObject
                { { "connection", connection }, { "serviceType", serviceType }, { "transportType", transportType }, { "protocol", protocol }, { "scenario", scenario }, { "result", "FAIL" }
                };

                SaveToFile(path, resFail);
                return;
            }

            // maybe success
            var percentage = GetSuccessPercentage(counters, scenario, connection);

            // todo: define what is success
            var result = "SUCCESS";
            // var result = percentage > _successThreshold ? "SUCCESS" : "FAIL";

            var res = new JObject
            { { "connection", connection }, { "serviceType", serviceType }, { "transportType", transportType }, { "protocol", protocol }, { "scenario", scenario }, { "result", result }
            };
            if (result == "FAIL")
            {
                SaveToFile(path, res);
                throw new Exception();
            }
            else
            {
                SaveToFile(path, res);
            }
        }

        private static void CheckLastJobResults(string path, int maxRetryCount, int connection, string serviceType,
            string transportType, string protocol, string scenario)
        {
            return;
        }

        private static int ParseMessageSize(string messageSizeStr)
        {
            var messageSize = 0;
            if (messageSizeStr.Contains("K") || messageSizeStr.Contains("k"))
                messageSize = Convert.ToInt32(messageSizeStr.Substring(0, messageSizeStr.Length - 1)) * 1024;
            else if (messageSizeStr.Contains("M") || messageSizeStr.Contains("m"))
                messageSize = Convert.ToInt32(messageSizeStr.Substring(0, messageSizeStr.Length - 1)) * 1024 * 1024;
            else
                messageSize = Convert.ToInt32(messageSizeStr);

            return messageSize;
        }

        private static BenchmarkCellConfig GenerateBenchmarkConfig(int indClient, string step,
            string serviceType, string transportType, string hubProtocol, string scenario,
            string MessageSizeStr, List<string> targetConnectionIds, List<string> groupNameList, List<bool> callbackList)
        {
            var messageSize = ParseMessageSize(MessageSizeStr);

            var benchmarkCellConfig = new BenchmarkCellConfig
            {
                ServiceType = serviceType,
                TransportType = transportType,
                HubProtocol = hubProtocol,
                Scenario = scenario,
                Step = step,
                MixEchoConnection = 0,
                MixBroadcastConnection = 0,
                MixGroupName = "",
                MixGroupConnection = 0,
                MessageSize = messageSize
            };

            // add lists
            benchmarkCellConfig.TargetConnectionIds.AddRange(targetConnectionIds);
            benchmarkCellConfig.GroupNameList.AddRange(groupNameList);
            benchmarkCellConfig.CallbackList.AddRange(callbackList);

            return benchmarkCellConfig;
        }

        private static void StartCollectCounters(List<RpcService.RpcServiceClient> clients, string outputSaveFile)
        {
            var collectTimer = new System.Timers.Timer(1000);
            collectTimer.AutoReset = true;
            collectTimer.Elapsed += async(sender, e) =>
            {
                var allClientCounters = new ConcurrentDictionary<string, ulong>();
                var collectCountersTasks = new List<Task>(clients.Count);
                var isSend = false;
                var isComplete = false;
                var swCollect = new Stopwatch();
                // Util.Log("\n\nstart collecting");
                swCollect.Start();
                for (var i = 0; i < clients.Count; i++)
                {
                    var ind = i;
                    collectCountersTasks.Add(Task.Run(async() =>
                    {
                        var state = clients[ind].GetState(new Empty { });

                        if ((int) state.State >= (int) Stat.Types.State.SendComplete) isComplete = true;

                        // if (false)
                        if ((int) state.State < (int) Stat.Types.State.SendRunning ||
                            (int) state.State > (int) Stat.Types.State.SendComplete && (int) state.State < (int) Stat.Types.State.HubconnDisconnecting)
                        {
                            return;
                        }
                        isSend = true;
                        isComplete = false;

                        var swRpc = new Stopwatch();
                        swRpc.Start();
                        var counters = await clients[ind].CollectCountersAsync(new Force { Force_ = false });
                        swRpc.Stop();
                        // Util.Log($"rpc time: {swRpc.Elapsed.TotalMilliseconds} ms");

                        for (var j = 0; j < counters.Pairs.Count; j++)
                        {
                            var key = counters.Pairs[j].Key;
                            var value = counters.Pairs[j].Value;
                            if (key.Contains("server"))
                            {
                                allClientCounters.AddOrUpdate(key, value, (k, v) => Math.Max(v, value));
                            }
                            else
                                allClientCounters.AddOrUpdate(key, value, (k, v) => v + value);
                        }
                    }));

                }

                await Task.WhenAll(collectCountersTasks);
                swCollect.Stop();
                Util.Log($"collecting counters time: {swCollect.Elapsed.TotalMilliseconds} ms");
                if (isSend == false || isComplete == true)
                {
                    return;
                }

                var jobj = new JObject();
                var received = (ulong) 0;
                foreach (var item in allClientCounters)
                {
                    jobj.Add(item.Key, item.Value);
                    if (item.Key.Contains("message") && (item.Key.Contains(":ge") || item.Key.Contains(":lt")))
                    {
                        received += item.Value;
                    }
                }

                jobj.Add("message:received", received);
                _counters = Util.Sort(jobj);
                var finalRec = new JObject
                { { "Time", Util.Timestamp2DateTimeStr(Util.Timestamp()) }, { "Counters", _counters }
                };
                string onelineRecord = Regex.Replace(finalRec.ToString(), @"\s+", "");
                onelineRecord = Regex.Replace(onelineRecord, @"\t|\n|\r", "");
                onelineRecord += "," + Environment.NewLine;
                Util.Log("per second: " + onelineRecord);

                try
                {
                    _writeLock.Wait();
                    // make sure only one process is writing
                    Util.SaveContentToFile(outputSaveFile, onelineRecord, true);
                }
                catch (Exception ex)
                {
                    Util.Log($"Cannot save file: {ex}");
                }
                finally
                {
                    _writeLock.Release();
                }
            };
            collectTimer.Start();
        }

        private static void WaitRpcConnectSuccess(List<RpcService.RpcServiceClient> clients)
        {
            while (true)
            {
                try
                {
                    foreach (var client in clients)
                    {
                        var strg = new Strg { Str = "" };
                        try
                        {
                            client.Test(strg);
                        }
                        catch (Exception)
                        {
                            Util.Log($"Fail to connect {client}");
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Util.Log($"rpc connection ex: {ex}");
                    continue;
                }
                break;
            }
        }

        private static ArgsOption ParseArgs(string[] args)
        {
            var argsOption = new ArgsOption();
            var result = Parser.Default.ParseArguments<ArgsOption>(args)
                .WithParsed(options => argsOption = options)
                .WithNotParsed(error => { });
            return argsOption;
        }

        private static List<string> ParseSlaveListStr(string slaveListStr)
        {
            return new List<string>(slaveListStr.Split(';'));
        }
        private static List<Channel> CreateRpcChannels(List<string> slaveList, int rpcPort, bool debug)
        {
            // open channel to rpc servers
            var channels = new List<Channel>(slaveList.Count);
            if (!debug)
            {
                for (var i = 0; i < slaveList.Count; i++)
                {
                    Util.Log($"add channel: {slaveList[i]}:{rpcPort}");
                    channels.Add(new Channel($"{slaveList[i]}:{rpcPort}", ChannelCredentials.Insecure));
                }
            }
            else
            {
                //debug
                channels.Add(new Channel($"{slaveList[0]}:5555", ChannelCredentials.Insecure));
                channels.Add(new Channel($"{slaveList[0]}:6666", ChannelCredentials.Insecure));
            }

            return channels;
        }

        private static List<RpcService.RpcServiceClient> CreateRpcClients(List<Channel> channels)
        {
            var clients = new List<RpcService.RpcServiceClient>();
            for (var i = 0; i < channels.Count; i++)
            {
                clients.Add(new RpcService.RpcServiceClient(channels[i]));
            }
            return clients;
        }

        private static void WaitChannelsShutDown(List<Channel> channels)
        {
            for (var i = 0; i < channels.Count; i++)
            {
                channels[i].ShutdownAsync().Wait();
            }
        }

        private static void ClientsLoadJobConfig(List<RpcService.RpcServiceClient> clients,
            List<string> slaveList, ArgsOption argsOption)
        {
            var connectionCnt = argsOption.Connections;
            var concurrentConnection = argsOption.ConcurrentConnection;
            var duration = argsOption.Duration;
            var interval = argsOption.Interval;
            var pipelineStr = argsOption.PipeLine;
            var serverUrl = argsOption.ServerUrl;
            var messageSizeStr = argsOption.MessageSize;

            var messageSize = ParseMessageSize(messageSizeStr);
            var servers = serverUrl.Split(';');
            var serverCount = servers.Length;
            var sendGroupCnt = argsOption.SendGroupCnt;

            clients.ForEach(client =>
            {
                var i = clients.IndexOf(client);
                var clientConnections = Util.SplitNumber(connectionCnt, i, slaveList.Count);
                var concurrentConnections = Util.SplitNumber(concurrentConnection, i, slaveList.Count);
                // modify the illegal case
                if (clientConnections > 0 && concurrentConnections == 0)
                {
                    Util.Log($"Warning: the concurrent connection '{concurrentConnection}' is too small, it is '{slaveList.Count}' at least");
                    concurrentConnections = 1;
                }
                else if (concurrentConnection > connectionCnt)
                {
                    Util.Log($"Warning: the concurrent connection '{concurrentConnection}' is too large, it is '{connectionCnt}' at most");
                    concurrentConnection = connectionCnt;
                }
                var state = new Stat();
                state = client.CreateWorker(new Empty());

                string server = null;
                if (bool.Parse(argsOption.sendToFixedClient))
                {
                    server = serverUrl;
                }
                else
                {
                    server = servers[i % serverCount];
                }
                var config = new CellJobConfig
                {
                    Connections = clientConnections,
                    ConcurrentConnections = concurrentConnections,
                    Interval = interval,
                    Duration = duration,
                    ServerUrl = server,
                    Pipeline = pipelineStr,
                    OneSend = i == clients.Count - 1 ? 1 : 0,
                    SendGroupCnt = sendGroupCnt
                };

                Util.Log($"create worker state: {state.State}");
                Util.Log($"client connections: {config.Connections}");
                state = client.LoadJobConfig(config);
                Util.Log($"load job config state: {state.State}");
            });
        }

        private static async Task ProcessPipeline(List<RpcService.RpcServiceClient> clients, string pipelineStr, List<string> slaveList, int connections,
            string serviceType, string transportType, string hubProtocol, string scenario, string messageSize,
            int groupNum, int overlap, int serverCount, string sendToFixedClient)
        {
            var pipeline = pipelineStr.Split(';').ToList();
            var connectionConfigBuilder = new ConnectionConfigBuilder();
            var connectionAllConfigList = connectionConfigBuilder.Build(connections);
            var targetConnectionIds = new List<string>();
            var groupNameList = GenerateGroupNameList(connections, groupNum, overlap);
            var callbackList = Enumerable.Repeat(true, connections).ToList();

            // var serverUrls = serverCount;
            for (var i = 0; i < pipeline.Count; i++)
            {

                var tasks = new List<Task>(clients.Count);
                var step = pipeline[i];
                int indClient = -1;
                Util.Log($"current step: {step}");

                // up op
                if (step.Substring(0, 2) == "up")
                {
                    var AdditionalSendConnCnt = 0;
                    var lastOne = false;
                    if (step.Substring(0, 2) == "up")
                    {
                        if (step.Contains("LastOne"))
                        {
                            AdditionalSendConnCnt = 1;
                            lastOne = true;
                        }
                        else
                        {
                            AdditionalSendConnCnt = Convert.ToInt32(step.Substring(2));
                        }
                    }

                    connectionAllConfigList = connectionConfigBuilder.UpdateSendConn(connectionAllConfigList, AdditionalSendConnCnt, connections, slaveList.Count, lastOne);
                }

                // update group name list
                var onlyOneSendAllGroup = step.Contains("configOnlyOneSendAllGroup") ? true : false;
                if (onlyOneSendAllGroup) groupNameList = UpdateGroupNameList(groupNameList);

                // remove last one callback
                RemoveExceptLastOneCallback(step, callbackList);

                clients.ForEach(client =>
                {
                    indClient++;

                    var benchmarkCellConfig = GenerateBenchmarkConfig(indClient, step,
                        serviceType, transportType, hubProtocol, scenario, messageSize,
                        targetConnectionIds, groupNameList, callbackList);

                    Util.Log($"service: {benchmarkCellConfig.ServiceType}; transport: {benchmarkCellConfig.TransportType}; hubprotocol: {benchmarkCellConfig.HubProtocol}; scenario: {benchmarkCellConfig.Scenario}; step: {step}");

                    var indClientInLoop = indClient;
                    tasks.Add(Task.Run(() =>
                    {
                        var beg = 0;
                        for (var indStart = 0; indStart < indClientInLoop; indStart++)
                        {
                            beg += Util.SplitNumber(connections, indStart, slaveList.Count);
                        }
                        var currConnSliceCnt = Util.SplitNumber(connections, indClientInLoop, slaveList.Count);

                        client.LoadConnectionRange(new Range { Begin = beg, End = beg + currConnSliceCnt });
                        client.LoadConnectionConfig(connectionAllConfigList);
                        client.RunJob(benchmarkCellConfig);
                    }));
                });
                await Task.WhenAll(tasks);
                await Task.Delay(1000);

                // collect all connections' ids just after connections start
                if (step.Contains("startConn"))
                {
                    // There are more than 1 server, we'd prefer to send message to target connection
                    // on different service, which means those message will go to Redis.
                    // In addition, in order to avoid time difference of different clients,
                    // The message should be sent to connections on the same clients.
                    if (bool.Parse(sendToFixedClient) && serverCount > 1)
                    {
                        targetConnectionIds = LeftShiftConnectionIdsOnEachClient(clients, serverCount);
                    }
                    else
                    {
                        targetConnectionIds = CollectConnectionIds(clients);
                        targetConnectionIds.Shuffle();
                    }
                }
            }
        }

        private static void RemoveExceptLastOneCallback(string step, List<bool> callbackList)
        {
            var removeExceptLastOneCallback = step.Contains("removeExceptLastOneCallback") ? true : false;
            if (removeExceptLastOneCallback) return;
            for (var i = 0; i < callbackList.Count; i++)
            {
                callbackList[i] = false;
            }
            callbackList[callbackList.Count - 1] = true;

        }

        private static List<string> UpdateGroupNameList(List<string> groupNameList)
        {
            var groupNameSet = new HashSet<string>();
            foreach (var groupNames in groupNameList)
            {
                foreach (var groupName in groupNames.Split(";").ToArray())
                {
                    groupNameSet.Add(groupName);
                }
            }
            groupNameList[groupNameList.Count - 1] = String.Join(";", groupNameSet.ToArray());
            return groupNameList;
        }

        private static void SavePid(string pidFile)
        {
            var pid = Process.GetCurrentProcess().Id;
            if (pidFile != null)
            {
                Util.SaveContentToFile(pidFile, Convert.ToString(pid), false);
            }
        }
    }
}