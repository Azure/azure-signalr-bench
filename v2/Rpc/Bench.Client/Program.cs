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
        public static void Main (string[] args)
        {
            // parse args
            var argsOption = new ArgsOption ();
            var result = Parser.Default.ParseArguments<ArgsOption> (args)
                .WithParsed (options => argsOption = options)
                .WithNotParsed (error => { });

            var pid = Process.GetCurrentProcess ().Id;
            if (argsOption.PidFile != null)
            {
                Util.SaveContentToFile (argsOption.PidFile, Convert.ToString (pid), false);
            }

            var slaveList = new List<string> (argsOption.SlaveList.Split (';'));

            // open channel to rpc servers
            var channels = new List<Channel> (slaveList.Count);
            if (argsOption.Debug != "debug")
            {
                for (var i = 0; i < slaveList.Count; i++)
                {
                    Util.Log ($"add channel: {slaveList[i]}:{argsOption.RpcPort}");
                    channels.Add (new Channel ($"{slaveList[i]}:{argsOption.RpcPort}", ChannelCredentials.Insecure));
                }
            }
            else
            {
                //debug
                channels.Add (new Channel ($"{slaveList[0]}:5555", ChannelCredentials.Insecure));
                channels.Add (new Channel ($"{slaveList[0]}:6666", ChannelCredentials.Insecure));
            }

            try
            {
                if (argsOption.Clear == "true")
                {
                    if (File.Exists (_jobResultFile))
                    {
                        File.Delete (_jobResultFile);
                    }
                }
                else
                {
                    if (File.Exists (_jobResultFile))
                    {
                        CheckLastJobResults (_jobResultFile, argsOption.Retry, argsOption.Connections,
                            argsOption.ServiceType, argsOption.TransportType, argsOption.HubProtocal, argsOption.Scenario);
                    }
                }
                // create rpc clients
                var clients = new List<RpcService.RpcServiceClient> (slaveList.Count);
                for (var i = 0; i < slaveList.Count; i++)
                {
                    clients.Add (new RpcService.RpcServiceClient (channels[i]));
                }

                // check rpc connections
                while (true)
                {
                    try
                    {
                        foreach (var client in clients)
                        {
                            var strg = new Strg { Str = "" };
                            client.Test (strg);
                        }
                    }
                    catch (Exception ex)
                    {
                        Util.Log ($"rpc connection ex: {ex}");
                        continue;
                    }
                    break;
                }
                // load job config
                var jobConfig = new JobConfig (argsOption);

                // call salves to load job config
                clients.ForEach (client =>
                {
                    var i = clients.IndexOf (client);
                    var clientConnections = Util.SplitNumber (argsOption.Connections, i, slaveList.Count);
                    var concurrentConnections = Util.SplitNumber (argsOption.ConcurrentConnection, i, slaveList.Count);
                    // modify the illegal case
                    if (clientConnections > 0 && concurrentConnections == 0)
                    {
                        Util.Log ($"Warning: the concurrent connection '{argsOption.ConcurrentConnection}' is too small, it is '{slaveList.Count}' at least");
                        concurrentConnections = 1;
                    }
                    var state = new Stat ();
                    state = client.CreateWorker (new Empty ());
                    var config = new CellJobConfig
                    {
                        Connections = clientConnections,
                        ConcurrentConnections = concurrentConnections,
                        Slaves = argsOption.Slaves,
                        Interval = argsOption.Interval,
                        Duration = argsOption.Duration,
                        ServerUrl = argsOption.ServerUrl,
                        Pipeline = argsOption.PipeLine
                    };
                    Util.Log ($"create worker state: {state.State}");
                    Util.Log ($"client connections: {config.Connections}");
                    state = client.LoadJobConfig (config);
                    Util.Log ($"load job config state: {state.State}");
                });

                // collect counters
                var collectTimer = new System.Timers.Timer (1000);
                collectTimer.AutoReset = true;
                collectTimer.Elapsed += (sender, e) =>
                {
                    var allClientCounters = new ConcurrentDictionary<string, int> ();
                    var collectCountersTasks = new List<Task> (clients.Count);
                    var isSend = false;
                    var isComplete = false;
                    var swCollect = new Stopwatch ();
                    swCollect.Start ();
                    clients.ForEach (client =>
                    {
                        var state = client.GetState (new Empty { });
                        if ((int) state.State >= (int) Stat.Types.State.SendComplete) isComplete = true;
                        if ((int) state.State < (int) Stat.Types.State.SendRunning ||
                            (int) state.State > (int) Stat.Types.State.SendComplete && (int) state.State < (int) Stat.Types.State.HubconnDisconnecting) return;
                        isSend = true;
                        isComplete = false;
                        var counters = client.CollectCounters (new Force { Force_ = false });

                        for (var i = 0; i < counters.Pairs.Count; i++)
                        {
                            var key = counters.Pairs[i].Key;
                            var value = counters.Pairs[i].Value;
                            if (key.Contains ("server"))
                            {
                                allClientCounters.AddOrUpdate (key, value, (k, v) => Math.Max (v, value));
                            }
                            else
                                allClientCounters.AddOrUpdate (key, value, (k, v) => v + value);
                        }
                    });
                    swCollect.Stop ();
                    Util.Log ($"collecting counters time: {swCollect.Elapsed.TotalSeconds} s");
                    if (isSend == false || isComplete == true)
                    {
                        return;
                    }

                    var jobj = new JObject ();
                    var received = 0;
                    foreach (var item in allClientCounters)
                    {
                        jobj.Add (item.Key, item.Value);
                        if (item.Key.Contains ("message") && (item.Key.Contains (":ge") || item.Key.Contains (":lt")))
                        {
                            received += item.Value;
                        }
                    }

                    jobj.Add ("message:received", received);
                    _counters = Util.Sort (jobj);
                    var finalRec = new JObject
                    { { "Time", Util.Timestamp2DateTimeStr (Util.Timestamp ()) }, { "Counters", _counters }
                    };
                    string onelineRecord = Regex.Replace (finalRec.ToString (), @"\s+", "");
                    onelineRecord = Regex.Replace (onelineRecord, @"\t|\n|\r", "");
                    onelineRecord += "," + Environment.NewLine;
                    Util.Log ("per second: " + onelineRecord);

                    try
                    {
                        Util.SaveContentToFile (argsOption.OutputCounterFile, onelineRecord, true);
                    }
                    catch (Exception ex)
                    {
                        Util.Log ($"Cannot save file: {ex}");
                    }
                };
                collectTimer.Start ();

                // process jobs for each step
                var pipeLines = new List<string> (argsOption.PipeLine.Split (';'));
                for (var i = 0; i < pipeLines.Count; i++)
                {
                    var tasks = new List<Task> (clients.Count);
                    var step = pipeLines[i];
                    int indClient = -1;
                    var connectionConfigBuilder = new ConnectionConfigBuilder ();
                    var connectionAllConfigList = connectionConfigBuilder.Build (argsOption.GroupConnection, argsOption.groupNum);

                    clients.ForEach (client =>
                    {
                        indClient++;
                        var mixEchoConn = Util.SplitNumber (argsOption.MixEchoConnection, indClient, slaveList.Count);
                        var mixBroadcastConn = Util.SplitNumber (argsOption.MixBroadcastConnection, indClient, slaveList.Count);
                        var mixGroupConn = Util.SplitNumber (argsOption.MixGroupConnection, indClient, slaveList.Count);
                        Util.Log ($"conn: echoConn {mixEchoConn}, b: {mixBroadcastConn}, g: {mixGroupConn}");

                        var benchmarkCellConfig = new BenchmarkCellConfig
                        {
                            ServiceType = argsOption.ServiceType,
                            TransportType = argsOption.TransportType,
                            HubProtocol = argsOption.HubProtocal,
                            Scenario = argsOption.Scenario,
                            Step = step,
                            MixEchoConnection = mixEchoConn,
                            MixBroadcastConnection = mixBroadcastConn,
                            MixGroupName = argsOption.MixGroupName,
                            MixGroupConnection = mixGroupConn
                        };

                        Util.Log ($"service: {benchmarkCellConfig.ServiceType}; transport: {benchmarkCellConfig.TransportType}; hubprotocol: {benchmarkCellConfig.HubProtocol}; scenario: {benchmarkCellConfig.Scenario}; step: {step}");

                        var indClientInLoop = indClient;
                        tasks.Add (Task.Run (() =>
                        {
                            var beg = 0;
                            for (var indStart = 0; indStart < indClientInLoop; indStart++)
                            {
                                Util.Log ($"indStart: {indStart}, indClient:{indClientInLoop}");
                                beg += Util.SplitNumber (argsOption.GroupConnection, indStart, slaveList.Count);
                            }
                            var currConnSliceCnt = Util.SplitNumber (argsOption.GroupConnection, indClientInLoop, slaveList.Count);
                            client.RunJob (benchmarkCellConfig);
                            client.LoadConnectionConfig (connectionAllConfigList);
                            Util.Log ($"range: ({beg}, {beg + currConnSliceCnt})");
                            client.LoadConnectionRange (new Range { Begin = beg, End = beg + currConnSliceCnt });
                        }));
                    });
                    Task.WhenAll (tasks).Wait ();
                    Task.Delay (1000).Wait ();
                }
            }
            catch (Exception ex)
            {
                Util.Log ($"Exception from RPC master: {ex}");
                SaveJobResult (_jobResultFile, null, argsOption.Connections, argsOption.ServiceType, argsOption.TransportType, argsOption.HubProtocal, argsOption.Scenario);
                throw;
            }
            SaveJobResult (_jobResultFile, _counters, argsOption.Connections, argsOption.ServiceType, argsOption.TransportType, argsOption.HubProtocal, argsOption.Scenario);

            for (var i = 0; i < channels.Count; i++)
            {
                channels[i].ShutdownAsync ().Wait ();
            }
            Console.WriteLine ("Exit client...");
        }

        private static void SaveConfig (string path, int connection, string serviceType, string transportType, string protocol, string scenario)
        {
            var jobj = new JObject
            { { "connection", connection }, { "serviceType", serviceType }, { "transportType", transportType }, { "protocol", protocol }, { "scenario", scenario }
            };

            string onelineRecord = Regex.Replace (jobj.ToString (), @"\s+", "");
            onelineRecord = Regex.Replace (onelineRecord, @"\t|\n|\r", "");
            onelineRecord += Environment.NewLine;

            Util.SaveContentToFile (path, onelineRecord, false);
        }

        private static void SaveToFile (string path, JObject jobj)
        {
            string onelineRecord = Regex.Replace (jobj.ToString (), @"\s+", "");
            onelineRecord = Regex.Replace (onelineRecord, @"\t|\n|\r", "");
            onelineRecord += Environment.NewLine;

            Util.SaveContentToFile (path, onelineRecord, true);
        }

        private static double GetSuccessPercentage (JObject counters, string scenario, int connection)
        {
            var sent = (int) counters["message:sent"];
            var notSent = (int) counters["message:notSentFromClient"];
            var total = sent + notSent;
            var received = (int) counters["message:received"];
            var percentage = 0.0;
            if (scenario.Contains ("broadcast"))
            {
                percentage = (double) received / (total * connection);
            }
            else if (scenario.Contains ("echo"))
            {
                percentage = (double) received / (total);
            }
            else if (scenario.Contains ("mix"))
            {
                percentage = 1.0; // todo
            }
            else if (scenario.Contains ("group"))
            {
                percentage = 1.0; // todo
            }

            return percentage;
        }

        private static void SaveJobResult (string path, JObject counters, int connection, string serviceType, string transportType, string protocol, string scenario)
        {
            // fail for sure
            if (counters == null)
            {
                var resFail = new JObject
                { { "connection", connection }, { "serviceType", serviceType }, { "transportType", transportType }, { "protocol", protocol }, { "scenario", scenario }, { "result", "FAIL" }
                };

                SaveToFile (path, resFail);
                return;
            }

            // maybe success
            var percentage = GetSuccessPercentage (counters, scenario, connection);
            var result = percentage > _successThreshold ? "SUCCESS" : "FAIL";

            var res = new JObject
            { { "connection", connection }, { "serviceType", serviceType }, { "transportType", transportType }, { "protocol", protocol }, { "scenario", scenario }, { "result", result }
            };
            if (result == "FAIL")
            {
                SaveToFile (path, res);
                throw new Exception ();
            }
            else
            {
                SaveToFile (path, res);
            }
        }

        private static void CheckLastJobResults (string path, int maxRetryCount, int connection, string serviceType,
            string transportType, string protocol, string scenario)
        {
            return;
            //var failCount = 0;
            //var lines = new List<string>(File.ReadAllLines(path));
            //for (var i = lines.Count - 1; i > lines.Count - 1 - maxRetryCount - 1  && i >= 0; i--)
            //{
            //    JObject res = null;
            //    try
            //    {
            //        res = JObject.Parse(lines[i]);
            //    }
            //    catch (Exception ex)
            //    {
            //        Util.Log($"parse result: {lines[i]}\n Exception: {ex}");
            //        continue;
            //    }
            //    if ((string)res["serviceType"] == serviceType &&
            //        (string)res["transportType"] == transportType && (string)res["protocol"] == protocol &&
            //        (string)res["scenario"] == scenario && (string)res["result"] == "FAIL")
            //    {
            //        failCount++;
            //    }
            //    else
            //    {
            //        break;
            //    }
            //}
            //Util.Log($"fail count: {failCount}");
            //if (failCount >= maxRetryCount)
            //{
            //    Util.Log("Too many fails. Break job");
            //    throw new Exception();
            //}

        }
    }
}