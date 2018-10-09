using System;
using CommandLine;

namespace Rpc.Slave
{
    class ArgsOption
    {
        // Rpc
        [Option("RpcPort", Required = false, Default = 5050, HelpText = "Port to be conencted from remote hosts. Default port is 5050.")]
        public int RpcPort { get; set; }

        [Option("HostName", Required = false, Default = "localhost", HelpText = "Hostname. Default hostname is 'localhost'.")]
        public string HostName{ get; set; }

        // Log
        [Option("LogName", Required = false, Default = "slave-.log", HelpText = "Log file name. Timestamp will insert into the position before dot. Default name is 'master-.log'. The final file name will be 'master-123456789.log', for example.")]
        public string LogName { get; set; }

        [Option("LogDirectory", Required = false, Default = ".", HelpText = "Directory in which the log will be stored. Default directory is '.'.")]
        public string LogDirectory { get; set; }
    }
}