using CommandLine;
using System;
using System.Threading.Tasks;

namespace DeployWebApp
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var argsOption = ParseArgs(args);
            var webappMgt = new WebAppManagement(argsOption);
            //webappMgt.GetAppPlanInformation();
            await webappMgt.Deploy();
        }

        private static ArgsOption ParseArgs(string[] args)
        {
            var argsOption = new ArgsOption();
            var result = Parser.Default.ParseArguments<ArgsOption>(args)
                .WithParsed(options => argsOption = options)
                .WithNotParsed(error =>
                {
                    Console.WriteLine($"Error in parsing arguments: {error}");
                    throw new ArgumentException($"Error in parsing arguments: {error}");
                });
            return argsOption;
        }
    }
}
