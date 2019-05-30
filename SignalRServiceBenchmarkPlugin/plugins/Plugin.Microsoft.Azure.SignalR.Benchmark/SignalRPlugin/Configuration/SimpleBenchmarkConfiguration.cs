using Plugin.Microsoft.Azure.SignalR.Benchmark.MasterMethods;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.RepresentationModel;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    public enum ConfigurationMode
    {
        Simple,
        Advance
    }

    public enum ConfigurationKind
    {
        perf,
        longrun
    }

    public enum ConfigurationConnectionType
    {
        Core,
        AspNet
    }

    public class SimpleBenchmarkConfiguration
    {
        protected static readonly string ModuleNameKey = "ModuleName";
        protected static readonly string PipelineKey = "Pipeline";
        protected static readonly string TypesKey = "Types";
        protected static readonly string ModeKey = "Mode";
        protected static readonly string KindKey = "Kind";
        protected static readonly string ConnectionTypeKey = "ConnectionType";

        // simple configuration keys
        protected static readonly string ConnectionKey = "Connections";
        protected static readonly string ArrivingRateKey = "ArrivingRate";
        protected static readonly string ProtocolKey = "Protocol";
        protected static readonly string TransportKey = "Transport";
        protected static readonly string WebAppTargetKey = "WebAppTarget";
        protected static readonly string ASRSConnectionStringKey = "ASRSConnectionString";
        protected static readonly string SingleStepDurationKey = "SingleStepDuration";
        protected static readonly string BaseSendingKey = "BaseSending";
        protected static readonly string SendingStepsKey = "SendingSteps";

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
            return true;
        }

        protected bool isPerf(YamlMappingNode root)
        {
            var keys = root.Children.Keys;
            if (keys.Contains(KindKey))
            {
                var mode = root.Children[new YamlScalarNode(KindKey)];
                if (Enum.TryParse(mode.ToString(), out ConfigurationKind m))
                {
                    return m == ConfigurationKind.perf;
                }
            }
            return true;
        }

        protected bool isCore(YamlMappingNode root)
        {
            var keys = root.Children.Keys;
            if (keys.Contains(ConnectionTypeKey))
            {
                var mode = root.Children[new YamlScalarNode(ConnectionTypeKey)];
                if (Enum.TryParse(mode.ToString(), out ConfigurationConnectionType m))
                {
                    return m == ConfigurationConnectionType.Core;
                }
            }
            return true;
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
            string transport)
        {
            var masterStep = CreateConnectionInternal(totalConnections, targetUrl, protocol, transport);
            return masterStep;
        }
    }
}
