using Plugin.Microsoft.Azure.SignalR.Benchmark.Internals;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    public class BenchmarkConfiguration : SimpleBenchmarkConfiguration
    {
        public IList<string> Types { get; set; } = new List<string>();

        public IList<IList<MasterStep>> Pipeline { get; set; } = new List<IList<MasterStep>>();

        public bool IsSimple { get; set; }

        public bool Debug { get; set; } = true;

        public BenchmarkConfiguration(string content)
        {
            Parse(content);
        }

        public void Dump()
        {
            Console.WriteLine($"Steps: {Pipeline.Count}");
            foreach (var parallelStep in Pipeline)
            {
                foreach (var step in parallelStep)
                {
                    step.Dump();
                }
            }
        }

        public static bool IsConfigInSimpleMode(string content)
        {
            // Setup input
            var input = new StringReader(content);

            // Load the stream
            var yaml = new YamlStream();
            yaml.Load(input);
            try
            {
                var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
                return IsSimpleNode(mapping);
            }
            catch (Exception ex)
            {
                Log.Error($"Benchmark configuration is invalid: {ex}");
                throw;
            }
        }

        public void Parse(string content)
        {
            // Setup input
            var input = new StringReader(content);

            // Load the stream
            var yaml = new YamlStream();
            yaml.Load(input);

            YamlMappingNode mapping = null;
            try
            {
                mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
                if (IsSimpleNode(mapping))
                {
                    HandleSimpleConfiguration(content);
                    IsSimple = true;
                }
                else
                {
                    HandleAdvanceConfiguration(mapping);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Benchmark configuration is invalid: {ex}");
                throw;
            }
            return;
        }

        public static bool IsResultParser(string content)
        {
            var simpleModel = new SimpleBenchmarkModel();
            var configData = simpleModel.Deserialize(content);
            if (!configData.isValid())
            {
                throw new Exception("Invalid benchmark configuration");
            }
            if (configData.isResultParser())
            {
                return true;
            }
            return false;
        }

        public static void ParseResult(string content)
        {
            var simpleModel = new SimpleBenchmarkModel();
            var configData = simpleModel.Deserialize(content);
            var percentileList = SignalRConstants.PERCENTILE_LIST.Split(",")
                                                     .Select(ind => Convert.ToDouble(ind)).ToArray();
            StatisticsParser.Parse(
                    configData.Config.ResultFilePath,
                    percentileList,
                    SignalRConstants.LATENCY_STEP,
                    SignalRConstants.LATENCY_MAX);
        }

        private void HandleSimpleConfiguration(string content)
        {
            var simpleModel = new SimpleBenchmarkModel();
            var configData = simpleModel.Deserialize(content);
            if (!configData.isValid())
            {
                throw new Exception("Invalid benchmark configuration");
            }
            string url = null;
            Debug = configData.IsDebug();
            if (configData.isResultParser())
            {
                return;
            }
            // create connections
            // Check REST API connection first
            if (configData.IsDirect())
            {
                url = configData.Config.ConnectionString;
                var masterStep = CreateDirectConnection(
                    configData.Config.Connections,
                    url,
                    configData.Config.Protocol,
                    configData.Config.Transport,
                    configData.Scenario.Name);
                AddSingleMasterStep(masterStep);
            }
            else if (configData.IsCore())
            {
                url = String.IsNullOrEmpty(configData.Config.WebAppTarget) ?
                    configData.Config.ConnectionString : configData.Config.WebAppTarget;
                var masterStep = CreateCoreConnection(
                    configData.Config.Connections,
                    url,
                    configData.Config.Protocol,
                    configData.Config.Transport,
                    configData.Scenario.Name);
                AddSingleMasterStep(masterStep);
            }
            else if (configData.IsAspNet())
            {
                url = configData.Config.WebAppTarget;
                var masterStep = CreateAspNetConnection(
                    configData.Config.Connections,
                    url,
                    configData.Config.Protocol,
                    configData.Config.Transport,
                    configData.Scenario.Name);
                AddSingleMasterStep(masterStep);
            }
            else
            {
                // error
            }
            // create statistics
            if (configData.isPerf())
            {
                var masterStep = InitStatisticsCollector(configData.Scenario.Name);
                AddSingleMasterStep(masterStep);
                masterStep = CollectStatistics(configData.Scenario.Name, configData.IsDebug(), configData.Config.ResultFilePath);
                AddSingleMasterStep(masterStep);
            }
            else if (configData.isLongrun())
            {
                var masterStep = InitConnectionStatisticsCollector(configData.Scenario.Name);
                AddSingleMasterStep(masterStep);
                masterStep = CollectConnectionStatistics(configData.Scenario.Name, configData.IsDebug(), configData.Config.ResultFilePath);
                AddSingleMasterStep(masterStep);
            }
            else
            {
                // error
            }
            // register callbacks
            if (configData.isPerf())
            {
                var masterStep = RegisterRecordLatency(configData.Scenario.Name);
                AddSingleMasterStep(masterStep);
            }
            else if (configData.isLongrun())
            {
                var masterStep = RegisterRecordLatency(configData.Scenario.Name);
                AddSingleMasterStep(masterStep);
                masterStep = RegisterOnConnected(configData.Scenario.Name);
                AddSingleMasterStep(masterStep);
            }
            // start connection
            AddSingleMasterStep(StartConnectin(configData.Config.ArrivingBatchMode,
                                               configData.Config.ArrivingRate,
                                               configData.Scenario.Name,
                                               configData.Config.ArrivingBatchWait));
            // wait for connections finish
            AddSingleMasterStep(Wait(configData.Scenario.Name));
            // reconnect if start connection has failures
            AddSingleMasterStep(Reconnect(configData.Config.Connections,
                                          url,
                                          configData.Config.Protocol,
                                          configData.Config.Transport,
                                          configData.Config.ArrivingBatchMode,
                                          configData.Scenario.Name,
                                          configData.Config.ArrivingRate));
            if (configData.Scenario.Name.EndsWith("Group"))
            {
                AddSingleMasterStep(JoinGroup(configData.Scenario.Name,
                                              configData.Scenario.Parameters.GroupCount,
                                              configData.Config.Connections));
                AddSingleMasterStep(Wait(configData.Scenario.Name));
            }
            else if (configData.Scenario.Name == "sendToClient")
            {
                AddSingleMasterStep(CollectConnectionId(configData.Scenario.Name));
                AddSingleMasterStep(Wait(configData.Scenario.Name));
                AddSingleMasterStep(Reconnect(configData.Config.Connections,
                                          url,
                                          configData.Config.Protocol,
                                          configData.Config.Transport,
                                          configData.Config.ArrivingBatchMode,
                                          configData.Scenario.Name,
                                          configData.Config.ArrivingRate));
                AddSingleMasterStep(Wait(configData.Scenario.Name));
                AddSingleMasterStep(CollectConnectionId(configData.Scenario.Name));
            }
            // setup backgroud task of repairing connections for longrun
            if (configData.isLongrun())
            {
                if (configData.Scenario.Name.EndsWith("Group"))
                {
                    AddSingleMasterStep(RepairConnections(configData.Scenario.Name, "JoinToGroup"));
                }
                else
                {
                    AddSingleMasterStep(RepairConnections(configData.Scenario.Name));
                }
            }
            // sending steps
            if (configData.Config.BaseSending > 0)
            {
                // Find the scenario method by name
                var scenario = configData.Scenario.Name;
                var scenarioNameLen = scenario.Length;
                var methodName = scenario.Substring(0, 1).ToUpper() + scenario.Substring(1, scenarioNameLen - 1);
                var scenarioMethod = GetType().GetMethod(methodName);
                // Calculate the steps
                var s = (configData.Config.Connections - configData.Config.BaseSending) / configData.Config.Step + 1;
                var steps = s;
                if (configData.Config.SendingSteps != SimpleBenchmarkModel.DEFAULT_SENDING_STEPS &&
                    steps < configData.Config.SendingSteps)
                {
                    steps = configData.Config.SendingSteps;
                }
                for (int i = 0; i < steps; i++)
                {
                    var endIndex = (uint)(configData.Config.BaseSending + i * configData.Config.Step);
                    if (i > 0)
                    {
                        // conditional stop and reconnect
                        if (configData.isPerf())
                        {
                            AddSingleMasterStep(ConditionalStop(configData.Scenario.Name,
                                                                configData.Config.Connections,
                                                                configData.Config.ConnectionFailPercentage,
                                                                configData.Config.LatencyPercentage));
                            AddSingleMasterStep(Reconnect(configData.Config.Connections,
                                                          url,
                                                          configData.Config.Protocol,
                                                          configData.Config.Transport,
                                                          configData.Config.ArrivingBatchMode,
                                                          configData.Scenario.Name,
                                                          configData.Config.ArrivingRate));
                            if (configData.Scenario.Name == "sendToClient")
                            {
                                AddSingleMasterStep(CollectConnectionId(configData.Scenario.Name));
                            }
                        }
                        AddSingleMasterStep(ConditionalStop(configData.Scenario.Name,
                                                            configData.Config.Connections,
                                                            configData.Config.ConnectionFailPercentage,
                                                            configData.Config.LatencyPercentage));
                    }
                    // sending scenario
                    var step = scenarioMethod.Invoke(this, new object[] { configData, endIndex });
                    AddSingleMasterStep((MasterStep)step);
                    AddSingleMasterStep(Wait(configData.Scenario.Name));
                }
                // post stop and clean steps
                if (configData.Scenario.Name.EndsWith("Group"))
                {
                    AddSingleMasterStep(LeaveGroup(configData.Scenario.Name,
                                                   configData.Scenario.Parameters.GroupCount,
                                                   configData.Config.Connections));
                }
                AddSingleMasterStep(StopCollector(configData.Scenario.Name));
                AddSingleMasterStep(StopConnection(configData.Scenario.Name));
                AddSingleMasterStep(DisposeConnection(configData.Scenario.Name));
            }
        }

        protected void AddSingleMasterStep(MasterStep masterStep)
        {
            var parallelSteps = new List<MasterStep>();
            parallelSteps.Add(masterStep);
            Pipeline.Add(parallelSteps);
        }

        private void HandleAdvanceConfiguration(YamlMappingNode root)
        {
            // handle advance configurations
            ValidateBenchmarkConfiguration(root);
            // Parse types
            var types = root.Children[new YamlScalarNode(TypesKey)] as YamlSequenceNode;
            foreach (var type in types)
            {
                Types.Add(type.ToString());
            }

            // Parse pipeline
            var pipelineNode = (YamlSequenceNode)root.Children[new YamlScalarNode(PipelineKey)] as YamlSequenceNode;
            foreach (var parallelStepNode in pipelineNode)
            {
                var parallelSteps = new List<MasterStep>();
                foreach (var stepNode in (YamlSequenceNode)parallelStepNode)
                {
                    var step = new MasterStep();
                    step.Parse((YamlMappingNode)stepNode);
                    parallelSteps.Add(step);
                }
                Pipeline.Add(parallelSteps);
            }
        }

        public void ValidateBenchmarkConfiguration(YamlMappingNode root)
        {
            var success = true;
            var keys = root.Children.Keys;
            success = keys.Contains(new YamlScalarNode(ModuleNameKey));
            if (!success)
            {
                Log.Error($"Module name is required, but not found.");
                throw new Exception($"Module name is required, but not found.");
            }
            success = keys.Contains(new YamlScalarNode(PipelineKey));
            if (!success)
            {
                Log.Error($"Pipeline is required, but not found.");
                throw new Exception($"Pipeline is required, but not found.");
            }
            success = keys.Contains(new YamlScalarNode(TypesKey));
            if (!success)
            {
                Log.Error($"Types is required, but not found.");
                throw new Exception($"Types is required, but not found.");
            }
        }
    }
}
