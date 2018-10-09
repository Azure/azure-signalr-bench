using System;
using CommandLine;

namespace Rpc.Slave
{
    class ArgsOption
    {
        // Rpc
        [Option("RpcPort", Required = false, Default = 5050, HelpText = "Port to be conencted from remote hosts.")]
        public int RpcPort { get; set; }

        [Option("HostName", Required = false, Default = "localhost", HelpText = "Host name.")]
        public string HostName{ get; set; }

        // Log
        [Option("LogName", Required = false, Default = "slave-.log", HelpText = "Log file name. Timestamp will insert into the position before dot.")]
        public string LogName { get; set; }

        [Option("LogDirectory", Required = false, Default = ".", HelpText = "Directory in which the log will be stored.")]
        public string LogDirectory { get; set; }
    }
}