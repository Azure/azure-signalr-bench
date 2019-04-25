using CommandLine;
using System;
using System.Threading.Tasks;

namespace DeployWebApp
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            await Run(args);
        }

        private static async Task RunDeploy(DeployOption deploy)
        {
            var webappMgt = new WebAppManagement(deploy);
            await webappMgt.Deploy();
        }

        private static Task RunGetInfo(GetInfoOption getInfoOption)
        {
            var webappMgt = new WebAppManagement(getInfoOption);
            webappMgt.GetAppPlanInformation();
            return Task.CompletedTask;
        }

        private static async Task RunDownloadLog(DownloadLogOption downloadLogOption)
        {
            var webappMgt = new WebAppManagement(downloadLogOption);
            try
            {
                await webappMgt.DownloadAppLog();
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.Message}");
            }
        }

        private static async Task RunRemoveGroup(RemoveGroupOption removeGroupOption)
        {
            var webappMgt = new WebAppManagement(removeGroupOption);
            await webappMgt.RemoveGroup();
        }

        private static async Task Run(string[] args)
        {
            var ret = Parser.Default.ParseArguments<DeployOption,
                                   GetInfoOption,
                                   DownloadLogOption,
                                   RemoveGroupOption>(args)
                                   .MapResult(
                (DeployOption opts) => RunDeploy(opts),
                (GetInfoOption opts) => RunGetInfo(opts),
                (DownloadLogOption opts) => RunDownloadLog(opts),
                (RemoveGroupOption opts) => RunRemoveGroup(opts),
                error => {
                    Console.WriteLine($"Error in parsing arguments: {error}");
                    return Task.CompletedTask;
                });
            await ret;
        }
    }
}
