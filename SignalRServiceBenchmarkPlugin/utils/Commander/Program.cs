using CommandLine;
using Common;
using Medallion.Shell;
using Renci.SshNet;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Commander
{
    class Program
    {
        private static string _baseName = "publish";

        public static void Main(string[] args)
        {
            var argsOption = ParseArgs(args);

            Util.CreateLogger(argsOption.LogDirectory, argsOption.LogName, argsOption.LogTarget);

            var automationTool = new AutomationTool(argsOption);
            //automationTool.Start();
            automationTool.StartDev();
        }

        private static ArgsOption ParseArgs(string[] args)
        {
            Log.Information($"Parse arguments...");
            var argsOption = new ArgsOption();
            var result = Parser.Default.ParseArguments<ArgsOption>(args)
                .WithParsed(options => argsOption = options)
                .WithNotParsed(error =>
                {
                    Log.Error($"Error in parsing arguments: {error}");
                    throw new ArgumentException($"Error in parsing arguments: {error}");
                });
            return argsOption;
        }
    }
}
