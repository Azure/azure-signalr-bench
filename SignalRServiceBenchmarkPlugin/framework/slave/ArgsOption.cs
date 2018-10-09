using System;
using CommandLine;

namespace Rpc.Slave
{
    class ArgsOption
    {
        [Option("RpcPort", Required = true, HelpText = "")]
        public int RpcPort { get; set; }

        [Option("SlaveList", Required = true, HelpText = "")]
        public string SlaveList { get; set; }

        [Option("HostName", Required = true, HelpText = "")]
        public string HostName{ get; set; }
    }
}