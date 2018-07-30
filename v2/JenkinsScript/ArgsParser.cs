using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;

namespace JenkinsScript
{
    public class ArgsOption
    {
        [Option ('a', "agentconfig", Required = false, HelpText = "Specify Agent Config File")]
        public string AgentConfigFile { get; set; }

        [Option ('j', "jobconfig", Required = false, HelpText = "Specify Job Config File")]
        public string JobConfigFile { get; set; }

        [Option ('C', "containername", Required = false, HelpText = "Specify Azure Container Name")]
        public string ContainerName { get; set; }

        [Option ('J', "jobblobname", Required = false, HelpText = "Specify Azure Blob Name For Job Config File")]
        public string JobBlobName { get; set; }

        [Option ('A', "agentblobname", Required = false, HelpText = "Specify Azure Blob Name For Agent Config File")]
        public string AgentBlobName { get; set; }

        [Option ('s', "azuresignalr", Required = false, HelpText = "Specify Azure Signalr connection string")]
        public string AzureSignalrConnectionString { get; set; }

        [Option ('o', "outputcounterfile", Required = false, HelpText = "Specify Output File For Counters")]
        public string OutputCounterFile { get; set; }

        [Option ('S', "step", Required = false, HelpText = "Specify the Step")]
        public string Step { get; set; }

        [Option ("ExtensionScriptsDir", Required = false, HelpText = "Specify the absolute directory of extension scripts")]
        public string ExtensionScriptDir { get; set; }

        [Option ("Unit", Required = false, Default = 2, HelpText = "Specify the unit number for SignalR Service")]
        public int SignalRUnit { get; set; }

        [Option ("ResourceGroup", Required = false, HelpText = "Specify the resource group if you want to delete SignalR Service")]
        public string ResourceGroup { get; set; }

        [Option ("SignalRService", Required = false, HelpText = "Specify the SignalR Service if you want to delete it")]
        public string SignalRService { get; set; }

        [Option ("ResourceGroupLocation", Required = false, Default = "southeastasia", HelpText = "Specify the Location of resource group")]
        public string Location { get; set; }

        [Option ('p', "spblobname", Required = false, HelpText = "Specify the Service Principal")]
        public string SpBlobName { get; set; }

        [Option ('h', "help", Required = false, HelpText = " dotnet run -j /home/wanl/workspace/signalr_auto_test_framework/signalr_bench/Rpc/Configs/job.yaml -a  /home/wanl/workspace/signalr_auto_test_framework/signalr_bench/Rpc/Configs/agent.yaml")]
        public string Help { get; set; }

        [Option ("debug", Required = false, HelpText = " debug")]
        public string Debug { get; set; }

        [Option ("utils", Required = false, Default = null, HelpText = "Specify the Utils.sh which is required by external scripts")]
        public string UtilsFilePath { get; set; }

        [Option ("commit", Default = "", Required = false, HelpText = "")]
        public string Commit { get; set; }

        [Option ("branch", Default = "master", Required = false, HelpText = "")]
        public string Branch { get; set; }

        [Option ("useLocalSignalR", Default = "false", Required = false, HelpText = "")]
        public string UseLocalSignalR { get; set; }

    }
}
