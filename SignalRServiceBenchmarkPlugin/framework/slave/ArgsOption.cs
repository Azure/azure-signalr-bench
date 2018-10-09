using System;
using CommandLine;

namespace Rpc.Slave
{
    class ArgsOption
    {
        [Option("RpcPort", Required = false, Default = 5050, HelpText = "")]
        public int RpcPort { get; set; }

        [Option("HostName", Required = false, Default = "localhost", HelpText = "")]
        public string HostName{ get; set; }
    }
}