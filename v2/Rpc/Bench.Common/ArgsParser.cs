using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;

namespace Bench.Common
{
    public class ArgsOption
    {
        [Option ('a', "agentconfig", Required = false, HelpText = "Specify Agent Config File")]
        public string AgentConfigFile { get; set; }

        [Option ('j', "jobconfig", Required = false, HelpText = "Specify Job Config File")]
        public string JobConfigFile { get; set; }

        [Option ('d', "dnsname", Default = "0.0.0.0", Required = false, HelpText = "Specify DNS Name")]
        public string DnsName { get; set; }

        [Option ('c', "containername", Required = false, HelpText = "Specify Azure Container Name")]
        public string ContainerName { get; set; }

        [Option ('y', "jobblobname", Required = false, HelpText = "Specify Azure Blob Name For Job Config File")]
        public string JobBlobName { get; set; }

        [Option ('x', "agentblobname", Required = false, HelpText = "Specify Azure Blob Name For Agent Config File")]
        public string AgentBlobName { get; set; }

        [Option ('o', "outputcounterfile", Required = false, HelpText = "Specify Output File For Counters")]
        public string OutputCounterFile { get; set; }

        [Option ('v', "servicetype", Required = false, HelpText = "Specify BenchMark Service Type")]
        public string ServiceType { get; set; }

        [Option ('t', "transporttype", Required = false, HelpText = "Specify TransportType")]
        public string TransportType { get; set; }

        [Option ('p', "hubprotocol", Required = false, HelpText = "Specify BenchMark Hub Protocol")]
        public string HubProtocal { get; set; }

        [Option ('s', "scenerio", Required = false, HelpText = "Specify BenchMark Scenario")]
        public string Scenario { get; set; }

        [Option ('B', "connectionbase", Required = false, HelpText = "Specify Connection Base")]
        public string ConnectionBase { get; set; }

        [Option ('S', "connectionincreasestep", Required = false, HelpText = "Specify Connection Increase Step")]
        public string ConnectionIncreaseStep { get; set; }

        [Option ("connections", Required = false, HelpText = "Specify Connection Increase Step")]
        public int Connections { get; set; }

        [Option ("duration", Required = false, HelpText = "Specify Connection Increase Step")]
        public int Duration { get; set; }

        [Option ("interval", Required = false, HelpText = "Specify Connection Increase Step")]
        public int Interval { get; set; }

        [Option ("slaves", Required = false, HelpText = "Specify Connection Increase Step")]
        public int Slaves { get; set; }

        [Option ("serverUrl", Required = false, HelpText = "Specify Connection Increase Step")]
        public string ServerUrl { get; set; }

        [Option ("pipeLine", Required = false, HelpText = "Specify Connection Increase Step")]
        public string PipeLine { get; set; }

        [Option ("rpcPort", Required = false, HelpText = "Specify Rpc Port")]
        public int RpcPort { get; set; }

        [Option ("slaveList", Required = false, HelpText = "Specify Slave List")]
        public string SlaveList { get; set; }

        [Option ("clear", Required = false, HelpText = "Clear Result File Or Not")]
        public string Clear { get; set; }

        [Option ("retry", Required = false, HelpText = "Set Max Retry Time")]
        public int Retry { get; set; }

        [Option ("concurrentConnection", Default = 1, Required = false, HelpText = "Set Concurrent connection")]
        public int ConcurrentConnection { get; set; }

        [Option ("pidFile", Default = null, Required = false, HelpText = "Set The File to Save PID")]
        public string PidFile { get; set; }

        [Option ("mixEchoConnection", Default = 0, Required = false, HelpText = "")]
        public int MixEchoConnection { get; set; }

        [Option ("mixBroadcastConnection", Default = 0, Required = false, HelpText = "")]
        public int MixBroadcastConnection { get; set; }

        [Option ("mixGroupConnection", Default = 0, Required = false, HelpText = "")]
        public int MixGroupConnection { get; set; }

        [Option ("mixGroupName", Required = false, HelpText = "")]
        public string MixGroupName { get; set; } = Guid.NewGuid ().ToString ("n").Substring (0, 8);

        [Option ("groupConnection", Required = false, HelpText = "")]
        public int GroupConnection { get; set; }

        [Option ("groupNum", Required = false, HelpText = "")]
        public int groupNum { get; set; }

        [Option ("debug", Required = false, HelpText = "")]
        public string Debug { get; set; }
    }
}