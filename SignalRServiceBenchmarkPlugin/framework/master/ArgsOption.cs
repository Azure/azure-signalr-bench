using CommandLine;
using System.Collections.Generic;

namespace Rpc.Master
{
    public class ArgsOption
    {
        // Rpc 
        [Option("RpcPort", Required = false, Default = 5050, HelpText = "")]
        public int RpcPort { get; set; }
        
        [Option("SlaveList", Required = false, Separator = ',', Default = new string[]{"localhost"}, HelpText = "")]
        public IList<string> SlaveList { get; set; }

        // Log
        [Option("LogName", Required = false, Default = "master-.log", HelpText = "")]
        public string LogName { get; set; }

        [Option("LogDirectory", Required = false, Default = ".", HelpText = "")]
        public string LogDirectory { get; set; }
    }
}