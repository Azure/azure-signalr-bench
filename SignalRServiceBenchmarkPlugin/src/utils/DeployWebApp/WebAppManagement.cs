using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.AppService.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DeployWebApp
{
    public class WebAppManagement : WebAppManagementBase
    {
        public WebAppManagement(CommonArgsOption argsOption) : base(argsOption)
        {
        }

        protected static async Task ScaleOutAppPlanAsync(
            (IAzure azure,
            string name,
            string groupName,
            int scaleOut) package)
        {
            var funcName = "ScaleOutAppPlanAsync";
            try
            {
                await ScaleOutAppPlanCoreAsync(package);
            }
            catch (Exception e)
            {
                Console.WriteLine($"{DateTime.Now.ToString("yyyyMMddHHmmss")} {funcName} failed {package.name} for {e.ToString()}");
            }
        }

        protected static async Task CreateAppPlan(
            (IAzure azure,
            string name,
            string region,
            string groupName,
            PricingTier pricingTier,
            Microsoft.Azure.Management.AppService.Fluent.OperatingSystem os) package)
        {
            var funcName = "CreateAppPlan";
            try
            {
                await CreateAppPlanCoreAsync(package);
            }
            catch (Exception e)
            {
                Console.WriteLine($"{DateTime.Now.ToString("yyyyMMddHHmmss")} {funcName} failed {package.name} for {e.ToString()}");
            }
        }

        protected static async Task CreateWebApp(
            (IAzure azure,
            string name,
            IAppServicePlan appServicePlan,
            IResourceGroup resourceGroup,
            string connectionString,
            string githubRepo,
            string serverConnectionCount) package)
        {
            var funcName = "CreateWebApp";
            try
            {
                await CreateWebAppCoreAsync(package);
            }
            catch (Exception e)
            {
                Console.WriteLine($"{DateTime.Now.ToString("yyyyMMddHHmmss")} {funcName} failed {package.name} for {e.ToString()}");
            }
        }

        public void GetAppPlanInformation()
        {
            Login();
            if (_argsOption is GetInfoOption)
            {
                var getInfoOption = (GetInfoOption)_argsOption;

                var iAppPlan = _azure.AppServices.AppServicePlans.GetByResourceGroup(_argsOption.GroupName, getInfoOption.AppPlanName);
                if (iAppPlan != null)
                {
                    Console.WriteLine($"number of webapps: {iAppPlan.NumberOfWebApps}");
                    Console.WriteLine($"capacity: {iAppPlan.Capacity}");
                    Console.WriteLine($"max instance: {iAppPlan.MaxInstances}");
                }
            }
        }

        public async Task DownloadAppLog()
        {
            Login();
            if (_argsOption is DownloadLogOption)
            {
                var downloadLogOption = (DownloadLogOption)_argsOption;
                var webApp = await _azure.WebApps.GetByIdAsync(downloadLogOption.WebAppResourceId);
                var pubProfile = await webApp.GetPublishingProfileAsync();
                if (!string.IsNullOrEmpty(pubProfile.FtpUrl) &&
                    !string.IsNullOrEmpty(pubProfile.FtpUsername) &&
                    !string.IsNullOrEmpty(pubProfile.FtpPassword))
                {
                    var ftpPrefix = "ftp://";
                    var url = pubProfile.FtpUrl;
                    var remoteFolder = "/site/wwwroot"; // Default folder
                    if (url.StartsWith(ftpPrefix))
                    {
                        var urlWoProtocol = url.Substring(ftpPrefix.Length);
                        url = urlWoProtocol;
                    }
                    if (url.IndexOf('/') > 0)
                    {
                        var slash = url.IndexOf('/');
                        if (slash > 0)
                        {
                            remoteFolder = url.Substring(slash);
                            var truncatedslash = url.Substring(0, slash);
                            url = truncatedslash;
                        }
                    }
                    Console.WriteLine($"FTP connect to {url} with {pubProfile.FtpUsername} and {pubProfile.FtpPassword}");
                    var ftpConnection = new FtpClientConnection(
                        url,
                        pubProfile.FtpUsername,
                        pubProfile.FtpPassword);
                    await ftpConnection.DownloadFile(
                        downloadLogOption.LogPrefix,
                        downloadLogOption.LogPostfix,
                        remoteFolder,
                        downloadLogOption.LocalLogFilePrefix);
                }
            }
        }

        private async Task CreateAppPlanAsync(List<string> appNameList)
        {
            if (_argsOption is DeployOption)
            {
                var deployOption = (DeployOption)_argsOption;
                // SDK for Linux docker does not support latest dotnet core runtime, so we have to use Windows
                var osType = Microsoft.Azure.Management.AppService.Fluent.OperatingSystem.Windows;
                var packages = (from i in Enumerable.Range(0, appNameList.Count)
                                where _azure.AppServices.AppServicePlans.GetByResourceGroup(_argsOption.GroupName, appNameList[i]) == null
                                select (azure: _azure,
                                        name: appNameList[i],
                                        region: deployOption.Location,
                                        groupName: _argsOption.GroupName,
                                        pricingTier: _targetPricingTier,
                                        os: osType)).ToList();
                Task Process<T>(T p) => BatchProcess(packages, CreateAppPlan, deployOption.ConcurrentCountOfServicePlan);
                bool NotReadyCheck<T>(T t) => isAnyServicePlanNotReady(appNameList, _argsOption.GroupName);
                await RetriableRun(packages, Process, NotReadyCheck, 60, "create app plan");
            }
        }

        private async Task CreateWebAppAsync(List<string> appNameList)
        {
            if (_argsOption is DeployOption)
            {
                var deployOption = (DeployOption)_argsOption;
                var packages = (from i in Enumerable.Range(0, appNameList.Count)
                                where _azure.WebApps.GetByResourceGroup(_argsOption.GroupName, appNameList[i]) == null
                                select (azure: _azure,
                                        name: appNameList[i],
                                        appServer: _azure.AppServices.AppServicePlans.GetByResourceGroup(_argsOption.GroupName, appNameList[i]),
                                        resourceGroup: _resourceGroup,
                                        connectionString: deployOption.ConnectionString,
                                        gitHubRepo: deployOption.GitHubRepo,
                                        serverConnectionCount: deployOption.ServerConnectionCount)).ToList();
                Task Process<T>(T p) => BatchProcess(packages, CreateWebApp, deployOption.ConcurrentCountOfWebApp);
                bool NotReadyCheck<T>(T t) => isAnyWebAppNotReady(appNameList, _argsOption.GroupName);
                await RetriableRun(packages, Process, NotReadyCheck, 60, "create webapp");
            }
        }

        private async Task ScaleOutAppPlan(List<string> appNameList)
        {
            if (_argsOption is DeployOption)
            {
                var deployOption = (DeployOption)_argsOption;
                var packages = (from i in Enumerable.Range(0, appNameList.Count)
                                where _azure.AppServices.AppServicePlans.GetByResourceGroup(_argsOption.GroupName, appNameList[i]) != null
                                select (azure: _azure,
                                        name: appNameList[i],
                                        groupName: _argsOption.GroupName,
                                        scaleOut: _scaleOut)).ToList();
                Task Process<T>(T p) => BatchProcess(packages, ScaleOutAppPlanAsync, deployOption.ConcurrentCountOfServicePlan);
                bool NotReadyCheck<T>(T t) => isAnyServicePlanScaleOutNotReady(appNameList, _argsOption.GroupName);
                await RetriableRun(packages, Process, NotReadyCheck, 120, "scale out app plan", 5);
            }
        }

        public async Task RemoveGroup()
        {
            Login();
            if (_argsOption is RemoveGroupOption)
            {
                await RemoveResourceGroup();
            }
        }

        public async Task Deploy()
        {
            Login();
            if (!ValidateDeployParameters())
            {
                return;
            }
            var deployOption = (DeployOption)_argsOption;
            var sw = new Stopwatch();
            sw.Start();
            _resourceGroup = await GetResourceGroup();
            // assign names
            var webappNameList = GenerateAppPlanNameList();
            if (!FindPricingTier(deployOption.PriceTier, out PricingTier targetPricingTier))
            {
                Console.WriteLine($"Unsupported pricing tier: {deployOption.PriceTier}");
                return;
            }
            _targetPricingTier = targetPricingTier;
            var groupName = _argsOption.GroupName;

            await CreateAppPlanAsync(webappNameList);
            sw.Stop();
            Console.WriteLine($"it takes {sw.ElapsedMilliseconds} ms to create app plan");

            sw.Start();
            await CreateWebAppAsync(webappNameList);
            sw.Stop();
            Console.WriteLine($"it takes {sw.ElapsedMilliseconds} ms to create webapp");

            sw.Start();
            await ScaleOutAppPlan(webappNameList);
            //scale outwebapp
            sw.Stop();
            Console.WriteLine($"it takes {sw.ElapsedMilliseconds} ms to scale out");
            // output app service plan Id
            DumpAppServicePlanId(webappNameList);
            // output scale out count
            DumpAppServicePlanScaleOutCount(webappNameList);
            // output web app Id
            DumpWebAppId(webappNameList);
            // dump results
            DumpWebAppUrl(webappNameList);
        }
    }
}
