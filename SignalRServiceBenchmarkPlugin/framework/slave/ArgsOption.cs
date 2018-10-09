using System;
using CommandLine;

namespace Rpc.Slave
{
    class ArgsOption
    {
        [Option("RpcPort", Required = true, Default = 5050, HelpText = "")]
        public int RpcPort { get; set; }

        [Option("HostName", Required = true, HelpText = "")]
        public string HostName{ get; set; }
    }
}