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
        private static double _successThreshold = 0.7;
        public static void Main(string[] args)
        {
            // parse args
            var argsOption = ParseArgs(args);

            // save pid
            SavePid(argsOption.PidFile);

            var slaveList = ParseSlaveListStr(argsOption.SlaveList);

            // generate rpc channels
            var channels = CreateRpcChannels(slaveList, argsOption.RpcPort, argsOption.Debug);

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
                ClientsLoadJobConfig(clients, argsOption.Connections, slaveList,
                    argsOption.ConcurrentConnection, argsOption.Duration, argsOption.Interval,
                    argsOption.PipeLine, argsOption.ServerUrl, argsOption.MessageSize);

                // collect counters
                StartCollectCounters(clients, argsOption.OutputCounterFile);

                // process jobs for each step
                ProcessPipeline(clients, argsOption.PipeLine, slaveList,
                    argsOption.Connections, argsOption.ServiceType, argsOption.TransportType, argsOption.HubProtocal, argsOption.Scenario, argsOption.MessageSize,
                    argsOption.groupNum, argsOption.groupOverlap);
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

        private static List<string> GenerateGroupNameList(int connCnt, int groupNum, int overlap)
        {
            var groupNameList = Enumerable.Repeat("", connCnt).ToList();
            for (var j = 0; j < overlap; j++)
            {
                for (var i = 0; i < groupNameList.Count; i++)
                {
                    if (groupNameList[i].Length > 0) groupNameList[i] += ";";
                    groupNameList[i] += $"group_{(i + j) % groupNum}";
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
            string serviceType, string transportType, string hubProtocol, string scenario, string MessageSizeStr,
            List<string> connectionIds, List<string> groupNameList)
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
            benchmarkCellConfig.TargetConnectionIds.AddRange(connectionIds);
            benchmarkCellConfig.GroupNameList.AddRange(groupNameList);
            return benchmarkCellConfig;
        }

        private static void StartCollectCounters(List<RpcService.RpcServiceClient> clients, string outputSaveFile)
        {
            var collectTimer = new System.Timers.Timer(1000);
            collectTimer.AutoReset = true;
            collectTimer.Elapsed += (sender, e) =>
            {
                var allClientCounters = new ConcurrentDictionary<string, double>();
                var collectCountersTasks = new List<Task>(clients.Count);
                var isSend = false;
                var isComplete = false;
                var swCollect = new Stopwatch();
                swCollect.Start();
                clients.ForEach(client =>
                {
                    var state = client.GetState(new Empty { });
                    if ((int) state.State >= (int) Stat.Types.State.SendComplete) isComplete = true;
                    if ((int) state.State < (int) Stat.Types.State.SendRunning ||
                        (int) state.State > (int) Stat.Types.State.SendComplete && (int) state.State < (int) Stat.Types.State.HubconnDisconnecting) return;
                    isSend = true;
                    isComplete = false;
                    var counters = client.CollectCounters(new Force { Force_ = false });

                    for (var i = 0; i < counters.Pairs.Count; i++)
                    {
                        var key = counters.Pairs[i].Key;
                        var value = counters.Pairs[i].Value;
                        if (key.Contains("server"))
                        {
                            allClientCounters.AddOrUpdate(key, value, (k, v) => Math.Max(v, value));
                        }
                        else
                            allClientCounters.AddOrUpdate(key, value, (k, v) => v + value);
                    }
                });
                swCollect.Stop();
                Util.Log($"collecting counters time: {swCollect.Elapsed.TotalSeconds} s");
                if (isSend == false || isComplete == true)
                {
                    return;
                }

                var jobj = new JObject();
                var received = 0.0;
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
                    Util.SaveContentToFile(outputSaveFile, onelineRecord, true);
                }
                catch (Exception ex)
                {
                    Util.Log($"Cannot save file: {ex}");
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
                        client.Test(strg);
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
        private static List<Channel> CreateRpcChannels(List<string> slaveList, int rpcPort, string debug)
        {
            // open channel to rpc servers
            var channels = new List<Channel>(slaveList.Count);
            if (debug != "debug")
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
            int connectionCnt, List<string> slaveList, int concurrentConnection, int duration,
            int interval, string pipelineStr, string serverUrl, string messageSizeStr)
        {
            var messageSize = ParseMessageSize(messageSizeStr);

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
                var state = new Stat();
                state = client.CreateWorker(new Empty());

                var config = new CellJobConfig
                {
                    Connections = clientConnections,
                    ConcurrentConnections = concurrentConnections,
                    Interval = interval,
                    Duration = duration,
                    ServerUrl = serverUrl,
                    Pipeline = pipelineStr
                };


                Util.Log($"create worker state: {state.State}");
                Util.Log($"client connections: {config.Connections}");
                state = client.LoadJobConfig(config);
                Util.Log($"load job config state: {state.State}");
            });
        }

        private static void ProcessPipeline(List<RpcService.RpcServiceClient> clients, string pipelineStr, List<string> slaveList, int connections,
            string serviceType, string transportType, string hubProtocol, string scenario, string messageSize,
            int groupNum, int overlap)
        {
            var pipeline = pipelineStr.Split(';').ToList();
            var connectionConfigBuilder = new ConnectionConfigBuilder();
            var connectionAllConfigList = connectionConfigBuilder.Build(connections);
            var connectionIds = new List<string>();
            var groupNameList = GenerateGroupNameList(connections, groupNum, overlap);
            for (var i = 0; i < pipeline.Count; i++)
            {
                var tasks = new List<Task>(clients.Count);
                var step = pipeline[i];
                int indClient = -1;
                var AdditionalSendConnCnt = (step.Contains("up")) ? Convert.ToInt32(step.Substring(2)) : 0;

                if (step.Contains("up"))
                {
                    Util.Log($"additional: {AdditionalSendConnCnt}");
                    connectionAllConfigList = connectionConfigBuilder.UpdateSendConn(connectionAllConfigList, AdditionalSendConnCnt, connections, slaveList.Count);
                }

                clients.ForEach(client =>
                {
                    indClient++;

                    var benchmarkCellConfig = GenerateBenchmarkConfig(indClient, step,
                        serviceType, transportType, hubProtocol, scenario, messageSize, connectionIds, groupNameList);

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
                Task.WhenAll(tasks).Wait();
                Task.Delay(1000).Wait();

                // collect all connections' ids just after connections start
                if (step.Contains("startConn"))
                {
                    connectionIds = CollectConnectionIds(clients);
                    connectionIds.Shuffle();
                }
            }
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