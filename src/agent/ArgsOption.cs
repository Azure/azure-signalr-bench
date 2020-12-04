using System;
using CommandLine;
using Common;

namespace Rpc.Agent
{
    class ArgsOption
    {
        // Rpc
        [Option("RpcPort", Required = false, Default = 8099, HelpText = "Port to be conencted from remote hosts.")]
        public int RpcPort { get; set; }

        [Option("PidFile", Required = false, Default = "agent-pid.txt")]
        public string PidFile { get; set; }

        [Option("HostName", Required = false, Default = "localhost", HelpText = "Hostname.")]
        public string HostName { get; set; }

        // Log
        [Option("LogName", Required = false, Default = "agent-.log", HelpText = "Log file name. " +
            "Timestamp will insert into the position before dot. If the name is 'agent-.log'. " +
            " The final file name will be 'agent-123456789.log', for example.")]
        public string LogName { get; set; }

        [Option("LogDirectory", Required = false, Default = ".", HelpText = "Directory in which the log will be stored.")]
        public string LogDirectory { get; set; }

        [Option("LogTarget", Required = false, Default = LogTargetEnum.All, HelpText = "Log target. " + 
            "Options: All/File/Console." + " All: Output to file and console;" + 
            " Console: Output to console;" + " File: Output to file.")]
        public LogTargetEnum LogTarget { get; set; }
    }
}
