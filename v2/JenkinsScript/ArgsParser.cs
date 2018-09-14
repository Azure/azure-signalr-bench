using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;

namespace JenkinsScript
{
    public class ArgsOption
    {
        [Option("AgentConfigFile", Required = false, HelpText = "Specify Agent Config File")]
        public string AgentConfigFile { get; set; }

        [Option('j', "jobconfig", Required = false, HelpText = "Specify Job Config File")]
        public string JobConfigFile { get; set; }

        [Option("JobConfigFileV2", Required = false, HelpText = "Specify Job Config File")]
        public string JobConfigFileV2 { get; set; }

        [Option('C', "containername", Required = false, HelpText = "Specify Azure Container Name")]
        public string ContainerName { get; set; }

        [Option('J', "jobblobname", Required = false, HelpText = "Specify Azure Blob Name For Job Config File")]
        public string JobBlobName { get; set; }

        [Option('A', "agentblobname", Required = false, HelpText = "Specify Azure Blob Name For Agent Config File")]
        public string AgentBlobName { get; set; }

        [Option('s', "AzureSignalrConnectionString", Default = "", Required = false, HelpText = "Specify Azure Signalr connection string")]
        public string AzureSignalrConnectionString { get; set; }

        [Option('o', "outputcounterfile", Required = false, HelpText = "Specify Output File For Counters")]
        public string OutputCounterFile { get; set; }

        [Option('S', "step", Required = false, HelpText = "Specify the Step")]
        public string Step { get; set; }

        [Option("ExtensionScriptsDir", Required = false, HelpText = "Specify the absolute directory of extension scripts")]
        public string ExtensionScriptDir { get; set; }

        [Option("Unit", Required = false, Default = 2, HelpText = "Specify the unit number for SignalR Service")]
        public int SignalRUnit { get; set; }

        [Option("ResourceGroup", Required = false, HelpText = "Specify the resource group if you want to delete SignalR Service")]
        public string ResourceGroup { get; set; }

        [Option("SignalRService", Required = false, HelpText = "Specify the SignalR Service if you want to delete it")]
        public string SignalRService { get; set; }

        [Option("ResourceGroupLocation", Required = false, Default = "southeastasia", HelpText = "Specify the Location of resource group")]
        public string Location { get; set; }

        [Option('p', "spblobname", Required = false, HelpText = "Specify the Service Principal")]
        public string SpBlobName { get; set; }

        [Option('h', "help", Required = false, HelpText = " dotnet run -j /home/wanl/workspace/signalr_auto_test_framework/signalr_bench/Rpc/Configs/job.yaml -a  /home/wanl/workspace/signalr_auto_test_framework/signalr_bench/Rpc/Configs/agent.yaml")]
        public string Help { get; set; }

        [Option("Debug", Required = false, HelpText = " debug")]
        public bool Debug { get; set; }

        [Option("utils", Required = false, Default = null, HelpText = "Specify the Utils.sh which is required by external scripts")]
        public string UtilsFilePath { get; set; }

        [Option("commit", Default = "", Required = false, HelpText = "")]
        public string Commit { get; set; }

        [Option("branch", Default = "origin/master", Required = false, HelpText = "")]
        public string Branch { get; set; }

        [Option("useLocalSignalR", Default = "false", Required = false, HelpText = "")]
        public string UseLocalSignalR { get; set; }

        [Option("PrivateIps", Default = "", Required = false, HelpText = "")]
        public string PrivateIps { get; set; }

        [Option("PublicIps", Default = "", Required = false, HelpText = "")]
        public string PublicIps { get; set; }

        [Option("PidFile", Default = "", Required = false, HelpText = "")]
        public string PidFile { get; set; }

        [Option("VnetGroupName", Default = "", Required = false, HelpText = "")]
        public string VnetGroupName { get; set; }

        [Option("VnetName", Default = "", Required = false, HelpText = "")]
        public string VnetName { get; set; }

        [Option("SubnetName", Default = "", Required = false, HelpText = "")]
        public string SubnetName { get; set; }

        [Option("sendToFixedClient", Default = "", Required = false, HelpText = "")]
        public string SendToFixedClient { get; set; }

        [Option("StatisticsSuffix", Default = "", Required = false, HelpText = "")]
        public string StatisticsSuffix { get; set; }

        [Option("KubeConfigFile", Default = "", Required = false, HelpText = "")]
        public string KubeConfigFile { get; set; }

        [Option("DisableRandomSuffix", Required = false, HelpText = "")]
        public bool DisableRandomSuffix { get; set; }

    }
}