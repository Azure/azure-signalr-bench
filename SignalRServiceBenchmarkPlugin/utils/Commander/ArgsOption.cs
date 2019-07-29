using CommandLine;
using Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Commander
{
    public class ArgsOption
    {
        // App server
        [Option("AppserverPort", Required = false, Default = 5050, HelpText = "Port of app server.")]
        public int AppserverPort { get; set; }

        [Option("AzureSignalRConnectionString", Required = false, HelpText = "Azure SignalR connection string.")]
        public string AzureSignalRConnectionString { get; set; }

        // Remote
        [Option("RpcPort", Required = false, Default = 5555, HelpText = "Port to be conencted from remote hosts.")]
        public int RpcPort { get; set; }

        [Option("AgentList", Required = true, Separator = ',',  HelpText = "Target hosts to connect.")]
        public IList<string> AgentList { get; set; }

        [Option("MasterHostname", Required = true, HelpText = "Hostname of master.")]
        public string MasterHostname { get; set; }

        // AspNet server does not need to start here
        [Option("AppServerHostnames", Required = false, Default = null, Separator = ',', HelpText = "Hostname of app server.")]
        public IList<string> AppServerHostnames { get; set; }

        [Option("AppServerCount", Default = 1000, Required = false, HelpText = "The most app server count you want to use. Default it is a max value: 1000")]
        public int AppServerCountInUse { get; set; }

        [Option("Username", Required = true, HelpText = "Username of VMs.")]
        public string Username { get; set; }

        [Option("Password", Required = true, HelpText = "Password of VMs.")]
        public string Password { get; set; }

        // Log
        [Option("LogName", Required = false, Default = "master-.log", HelpText = "Log file name. Timestamp will insert into the position before dot. If the name is 'master-.log'. The final file name will be 'master-123456789.log' for example.")]
        public string LogName { get; set; }

        [Option("LogDirectory", Required = false, Default = ".", HelpText = "Directory in which the log will be stored.")]
        public string LogDirectory { get; set; }

        [Option("LogTarget", Required = false, Default = LogTargetEnum.All, HelpText = "Log target. " +
            "Options: All/File/Console." + " All: Output to file and console;" +
            " Console: Output to console;" + " File: Output to file.")]
        public LogTargetEnum LogTarget { get; set; }

        // Project
        // AspNet server does not need to start here
        [Option("AppserverProject", Required = false, HelpText = "App server project.")]
        public string AppserverProject { get; set; }

        [Option("AppserverLogDirectory", Required = false, Default = ".", HelpText = "Specify the directory path to save appserver log")]
        public string AppserverLogDirectory { get; set; }

        [Option("MasterProject", Required = true, HelpText = "Master project.")]
        public string MasterProject { get; set; }

        [Option("AgentProject", Required = true, HelpText = "Agent project.")]
        public string AgentProject { get; set; }

        [Option("AppserverTargetPath", Required = false, Default = null, HelpText = "Target path for app server executable.")]
        public string AppserverTargetPath { get; set; }

        [Option("MasterTargetPath", Required = true, HelpText = "Target path for master executable.")]
        public string MasterTargetPath { get; set; }

        [Option("AgentTargetPath", Required = true, HelpText = "Target path for agent executables.")]
        public string AgentTargetPath { get; set; }

        [Option("BenchmarkConfiguration", Required = true, HelpText = "Benchmark configurations.")]
        public string BenchmarkConfiguration { get; set; }

        [Option("BenchmarkConfigurationTargetPath", Required = true, HelpText = "Benchmark configurations target path.")]
        public string BenchmarkConfigurationTargetPath { get; set; }

        // Performance mode
        [Option("UserMode", Required = false, HelpText = "User mode.")]
        public bool UserMode { get; set; }

        [Option("NotStartAppServer", Required = false, Default = 0, HelpText = "Do not start app server, ApsNet server is started by another exe, default is 0")]
        public int NotStartAppServer { get; set; }

        [Option("NotStopAppServer", Required = false, Default = 0, HelpText = "Do not stop app server, default is 0")]
        public int NotStopAppServer { get; set; }
    }
}
