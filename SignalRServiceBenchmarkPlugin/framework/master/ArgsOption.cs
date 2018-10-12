﻿using CommandLine;
using Common;
using System.Collections.Generic;

namespace Rpc.Master
{
    public class ArgsOption
    {
        // Rpc 
        [Option("RpcPort", Required = false, Default = 5050, HelpText = "Port to connect to the remote host.")]
        public int RpcPort { get; set; }
        
        [Option("SlaveList", Required = false, Separator = ',', Default = new string[]{"localhost"}, HelpText = "Target hosts to connect.")]
        public IList<string> SlaveList { get; set; }

        // Log
        [Option("LogName", Required = false, Default = "master-.log", HelpText = "Log file name. Timestamp will insert into the position before dot. If the name is 'master-.log'. The final file name will be 'master-123456789.log' for example.")]
        public string LogName { get; set; }

        [Option("LogDirectory", Required = false, Default = ".", HelpText = "Directory in which the log will be stored.")]
        public string LogDirectory { get; set; }

        [Option("LogTarget", Required = false, Default = LogTargetEnum.All, HelpText = "Log target. " +
            "Options: All/File/Console." + " All: Output to file and console;" +
            " Console: Output to console;" + " File: Output to file.")]
        public LogTargetEnum LogTarget { get; set; }

        // Benchmark configuration
        [Option("BenchmarkConfiguration", Required = true,  HelpText = "Benchmark configuration Path.")]
        public string BenchmarkConfiguration { get; set; }
    }
}