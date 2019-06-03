using Plugin.Microsoft.Azure.SignalR.Benchmark.MasterMethods;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.RepresentationModel;
using static Plugin.Microsoft.Azure.SignalR.Benchmark.SimpleBenchmarkModel;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    public enum ConfigurationMode
    {
        Simple,
        Advance
    }

    public class SimpleBenchmarkConfiguration
    {
        protected static readonly string ModuleNameKey = "ModuleName";
        protected static readonly string PipelineKey = "Pipeline";
        protected static readonly string TypesKey = "Types";
        protected static readonly string ModeKey = "Mode";

        // default settings
        protected static readonly int DEFAULT_CONNECTIONS = 1000;
        protected static readonly int DEFAULT_ARRIVINGRATE = 50;
        protected static readonly int DEFAULT_MESSAGESIZE = 2048;
        protected static readonly string DEFAULT_TRANSPORT = "Websockets";
        protected static readonly string DEFAULT_PROTOCOL = "json";

        protected string ConnectionString;
        protected string WebAppTarget;
        protected string Transport;
        protected string Protocol;
        protected string Scenario;
        protected int Connections;
        protected int ArrivingRate;
        protected int SingleStepDuration;
        protected int BaseSending;
        protected int SendingSteps;

        protected bool isSimple(YamlMappingNode root)
        {
            var keys = root.Children.Keys;
            if (keys.Contains(ModeKey))
            {
                var mode = root.Children[new YamlScalarNode(ModeKey)];
                if (Enum.TryParse(mode.ToString(), out ConfigurationMode m))
                {
                    return m == ConfigurationMode.Simple;
                }
            }
            return false;
        }

        protected MasterStep AttachType(MasterStep masterStep, string typeName)
        {
            masterStep.Parameters[Plugin.Base.Constants.Type] = typeName;
            return masterStep;
        }

        protected MasterStep RegisterRecordLatency(string typeName)
        {
            var masterStep = new MasterStep();
            masterStep.Parameters[Plugin.Base.Constants.Method] = typeof(RegisterCallbackRecordLatency).Name;
            AttachType(masterStep, typeName);
            return masterStep;
        }

        protected MasterStep RegisterOnConnected(string typeName)
        {
            var masterStep = new MasterStep();
            masterStep.Parameters[Plugin.Base.Constants.Method] = typeof(RegisterCallbackOnConnected).Name;
            AttachType(masterStep, typeName);
            return masterStep;
        }

        protected MasterStep CreateDirectConnection(
            int totalConnections,
            string targetUrl,
            string protocol,
            string transport)
        {
            var masterStep = CreateConnectionInternal(totalConnections, targetUrl, protocol, transport);
            masterStep.Parameters[Plugin.Base.Constants.Method] = typeof(CreateDirectConnection).Name;
            return masterStep;
        }

        protected MasterStep CreateAspNetConnection(
            int totalConnections,
            string targetUrl,
            string protocol,
            string transport)
        {
            var masterStep = CreateConnectionInternal(totalConnections, targetUrl, protocol, transport);
            masterStep.Parameters[Plugin.Base.Constants.Method] = typeof(CreateAspNetConnection).Name;
            return masterStep;
        }

        protected MasterStep CreateCoreConnection(
            int totalConnections,
            string targetUrl,
            string protocol,
            string transport)
        {
            var masterStep = CreateConnectionInternal(totalConnections, targetUrl, protocol, transport);
            masterStep.Parameters[Plugin.Base.Constants.Method] = typeof(CreateConnection).Name;
            return masterStep;
        }

        protected MasterStep CreateConnectionInternal(
            int totalConnections,
            string targetUrl,
            string protocol,
            string transport)
        {
            var masterStep = new MasterStep();
            masterStep.Parameters[SignalRConstants.ConnectionTotal] = totalConnections;
            masterStep.Parameters[SignalRConstants.HubProtocol] = protocol;
            masterStep.Parameters[SignalRConstants.TransportType] = transport;
            masterStep.Parameters[SignalRConstants.HubUrls] = targetUrl;
            return masterStep;
        }

        protected MasterStep InitStatisticsCollector(string typeName)
        {
            var masterStep = InitStatisticsCollectorInternal(typeName);
            masterStep.Parameters[Plugin.Base.Constants.Method] = typeof(InitStatisticsCollector).Name;
            return masterStep;
        }

        protected MasterStep InitConnectionStatisticsCollector(string typeName)
        {
            var masterStep = InitStatisticsCollectorInternal(typeName);
            masterStep.Parameters[Plugin.Base.Constants.Method] = typeof(InitConnectionStatisticsCollector).Name;
            return masterStep;
        }

        protected MasterStep InitStatisticsCollectorInternal(string typeName)
        {
            var masterStep = new MasterStep();
            masterStep = AttachType(masterStep, typeName);
            masterStep.Parameters[$"{SignalRConstants.LatencyStep}.{typeName}"] = SignalRConstants.LATENCY_STEP;
            masterStep.Parameters[$"{SignalRConstants.LatencyMax}.{typeName}"] = SignalRConstants.LATENCY_MAX;
            return masterStep;
        }

        protected MasterStep CollectStatistics(
            string typeName,
            int interval = 1000,
            string output = "counters.txt")
        {
            var masterStep = CollectStatisticsInternal(typeName, interval, output);
            masterStep.Parameters[Plugin.Base.Constants.Method] = typeof(CollectStatistics).Name;
            return masterStep;
        }

        protected MasterStep CollectConnectionStatistics(
            string typeName,
            int interval = 1000,
            string output = "counters.txt",
            string percentileList = SignalRConstants.PERCENTILE_LIST)
        {
            var masterStep = CollectStatisticsInternal(typeName, interval, output);
            masterStep.Parameters[SignalRConstants.PercentileList] = percentileList;
            masterStep.Parameters[Plugin.Base.Constants.Method] = typeof(CollectStatistics).Name;
            return masterStep;
        }

        protected MasterStep CollectStatisticsInternal(
            string typeName,
            int interval = 1000,
            string output = "counters.txt")
        {
            var masterStep = new MasterStep();
            masterStep = AttachType(masterStep, typeName);
            masterStep.Parameters[SignalRConstants.Interval] = 1000;
            masterStep.Parameters[SignalRConstants.StatisticsOutputPath] = output;
            return masterStep;
        }

        protected MasterStep StartConnectin(
            string batchMode,
            int concurrent,
            string typeName,
            int wait = 1000)
        {
            var masterStep = new MasterStep();
            masterStep.Parameters[Plugin.Base.Constants.Method] = typeof(StartConnection).Name;
            masterStep.Parameters[SignalRConstants.ConcurrentConnection] = concurrent;
            masterStep.Parameters[SignalRConstants.BatchMode] = batchMode;
            masterStep.Parameters[SignalRConstants.BatchWait] = wait;
            masterStep = AttachType(masterStep, typeName);
            return masterStep;
        }

        protected MasterStep Wait(
            string typeName,
            int wait = 5000)
        {
            var masterStep = new MasterStep();
            masterStep.Parameters[Plugin.Base.Constants.Method] = typeof(Wait).Name;
            masterStep.Parameters[SignalRConstants.Duration] = wait;
            masterStep = AttachType(masterStep, typeName);
            return masterStep;
        }

        protected MasterStep Reconnect(
            int totalConnections,
            string targetUrl,
            string protocol,
            string transport,
            string batchMode,
            string typeName,
            int concurrent,
            int wait = 1000)
        {
            var masterStep = CreateConnectionInternal(totalConnections, targetUrl, protocol, transport);
            masterStep.Parameters[Plugin.Base.Constants.Method] = typeof(Reconnect).Name;
            masterStep.Parameters[SignalRConstants.ConcurrentConnection] = concurrent;
            masterStep.Parameters[SignalRConstants.BatchMode] = batchMode;
            masterStep.Parameters[SignalRConstants.BatchWait] = wait;
            masterStep = AttachType(masterStep, typeName);
            return masterStep;
        }

        protected MasterStep ConditionalStop(
            string typeName,
            int maxConnections,
            double connectionFailPercentage,
            double latencyPercentage)
        {
            var masterStep = new MasterStep();
            masterStep.Parameters[Plugin.Base.Constants.Method] = typeof(ConditionalStop).Name;
            masterStep.Parameters[SignalRConstants.CriteriaMaxFailConnectionAmount] = maxConnections;
            masterStep.Parameters[SignalRConstants.CriteriaMaxFailConnectionPercentage] = connectionFailPercentage;
            masterStep.Parameters[SignalRConstants.CriteriaMaxFailSendingPercentage] = latencyPercentage;
            masterStep = AttachType(masterStep, typeName);
            return masterStep;
        }

        protected MasterStep CollectConnectionId(string typeName)
        {
            var masterStep = new MasterStep();
            masterStep.Parameters[Plugin.Base.Constants.Method] = typeof(CollectConnectionId).Name;
            masterStep = AttachType(masterStep, typeName);
            return masterStep;
        }

        protected MasterStep Broadcast(BenchConfigData config)
        {
            return Broadcast(
                config.Scenario.Name,
                config.Config.SingleStepDuration,
                config.Scenario.Parameters.MessageSize,
                config.Scenario.Parameters.SendingInterval,
                config.Config.Connections,
                0,
                config.Config.SendingSteps);
        }

        protected MasterStep Broadcast(
            string typeName,
            int stepDuration,
            int msgSize,
            int sendingInterval,
            int totalConnections,
            int beginIndex,
            int endIndex)
        {
            var masterStep = SimpleSendingScenario(stepDuration, msgSize, sendingInterval, totalConnections, beginIndex, endIndex);
            masterStep.Parameters[Plugin.Base.Constants.Method] = typeof(Broadcast).Name;
            masterStep = AttachType(masterStep, typeName);
            return masterStep;
        }

        protected MasterStep Echo(
            string typeName,
            int stepDuration,
            int msgSize,
            int sendingInterval,
            int totalConnections,
            int beginIndex,
            int endIndex)
        {
            var masterStep = SimpleSendingScenario(stepDuration, msgSize, sendingInterval, totalConnections, beginIndex, endIndex);
            masterStep.Parameters[Plugin.Base.Constants.Method] = typeof(Echo).Name;
            masterStep = AttachType(masterStep, typeName);
            return masterStep;
        }

        protected MasterStep SendToClient(
            string typeName,
            int stepDuration,
            int msgSize,
            int sendingInterval,
            int totalConnections,
            int beginIndex,
            int endIndex)
        {
            var masterStep = SimpleSendingScenario(stepDuration, msgSize, sendingInterval, totalConnections, beginIndex, endIndex);
            masterStep.Parameters[SignalRConstants.ConnectionTotal] = totalConnections;
            masterStep.Parameters[Plugin.Base.Constants.Method] = typeof(SendToClient).Name;
            masterStep = AttachType(masterStep, typeName);
            return masterStep;
        }

        protected MasterStep SendToGroup(
            string typeName,
            int groupCount,
            int stepDuration,
            int msgSize,
            int sendingInterval,
            int totalConnections,
            int beginIndex,
            int endIndex)
        {
            var masterStep = SimpleSendingScenario(stepDuration, msgSize, sendingInterval, totalConnections, beginIndex, endIndex);
            masterStep.Parameters[SignalRConstants.GroupConfigMode] = SignalREnums.GroupConfigMode.Connection.ToString();
            masterStep.Parameters[SignalRConstants.Modulo] = totalConnections;
            masterStep.Parameters[SignalRConstants.GroupCount] = groupCount;
            masterStep.Parameters[SignalRConstants.ConnectionTotal] = totalConnections;
            masterStep.Parameters[Plugin.Base.Constants.Method] = typeof(SendToGroup).Name;
            masterStep = AttachType(masterStep, typeName);
            return masterStep;
        }

        protected MasterStep SimpleSendingScenario(
            int stepDuration,
            int msgSize,
            int sendingInterval,
            int totalConnections,
            int beginIndex,
            int endIndex)
        {
            var masterStep = new MasterStep();
            masterStep.Parameters[SignalRConstants.RemainderBegin] = beginIndex;
            masterStep.Parameters[SignalRConstants.RemainderEnd] = endIndex;
            masterStep.Parameters[SignalRConstants.Modulo] = totalConnections;
            masterStep.Parameters[SignalRConstants.Duration] = stepDuration;
            masterStep.Parameters[SignalRConstants.MessageSize] = msgSize;
            masterStep.Parameters[SignalRConstants.Interval] = sendingInterval;
            return masterStep;
        }

        protected MasterStep JoinGroup(
            string typeName,
            int groupCount,
            int connections)
        {
            var masterStep = GroupInternal(typeName, groupCount, connections);
            masterStep.Parameters[Plugin.Base.Constants.Method] = typeof(JoinGroup).Name;
            return masterStep;
        }

        protected MasterStep LeaveGroup(
            string typeName,
            int groupCount,
            int connections)
        {
            var masterStep = GroupInternal(typeName, groupCount, connections);
            masterStep.Parameters[Plugin.Base.Constants.Method] = typeof(LeaveGroup).Name;
            return masterStep;
        }

        protected MasterStep GroupInternal(
            string typeName,
            int groupCount,
            int connections)
        {
            var masterStep = new MasterStep();
            masterStep.Parameters[Plugin.Base.Constants.Method] = typeof(LeaveGroup).Name;
            masterStep.Parameters[SignalRConstants.GroupCount] = groupCount;
            masterStep.Parameters[SignalRConstants.ConnectionTotal] = connections;
            masterStep = AttachType(masterStep, typeName);
            return masterStep;
        }
    }
}
