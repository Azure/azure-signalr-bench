using System;
using CommandLine;

namespace Rpc.Slave
{
    class ArgsOption
    {
        // Rpc
        [Option("RpcPort", Required = false, Default = 5050, HelpText = "")]
        public int RpcPort { get; set; }

        [Option("HostName", Required = false, Default = "localhost", HelpText = "")]
        public string HostName{ get; set; }

        // Log
        [Option("LogName", Required = false, Default = "slave-.log", HelpText = "")]
        public string LogName { get; set; }

        [Option("LogDirectory", Required = false, Default = ".", HelpText = "")]
        public string LogDirectory { get; set; }
    }
}