using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace DeployWebApp
{
    public class WebAppManagement : WebAppManagementBase
    {
        public WebAppManagement(ArgsOption argsOption) : base(argsOption)
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
            var iAppPlan = _azure.AppServices.AppServicePlans.GetByResourceGroup(_argsOption.GroupName, _argsOption.AppPlanName);
            if (iAppPlan != null)
            {
                Console.WriteLine($"number of webapps: {iAppPlan.NumberOfWebApps}");
                Console.WriteLine($"capacity: {iAppPlan.Capacity}");
                Console.WriteLine($"max instance: {iAppPlan.MaxInstances}");
            }
        }

        private async Task CreateAppPlanAsync(List<string> appNameList)
        {
            var packages = (from i in Enumerable.Range(0, appNameList.Count)
                            where _azure.AppServices.AppServicePlans.GetByResourceGroup(_argsOption.GroupName, appNameList[i]) == null
                            select (azure: _azure,
                                    name: appNameList[i],
                                    region: _argsOption.Location,
                                    groupName: _argsOption.GroupName,
                                    pricingTier: _targetPricingTier,
                                    os: Microsoft.Azure.Management.AppService.Fluent.OperatingSystem.Windows)).ToList();
            Task Process<T>(T p) => BatchProcess(packages, CreateAppPlan, _argsOption.ConcurrentCountOfServicePlan);
            bool NotReadyCheck<T>(T t) => isAnyServicePlanNotReady(appNameList, _argsOption.GroupName);
            await RetriableRun(packages, Process, NotReadyCheck, 60, "create app plan");
        }

        private async Task CreateWebAppAsync(List<string> appNameList)
        {
            var packages = (from i in Enumerable.Range(0, appNameList.Count)
                            where _azure.WebApps.GetByResourceGroup(_argsOption.GroupName, appNameList[i]) == null
                            select (azure: _azure,
                                    name: appNameList[i],
                                    appServer: _azure.AppServices.AppServicePlans.GetByResourceGroup(_argsOption.GroupName, appNameList[i]),
                                    resourceGroup: _resourceGroup,
                                    connectionString: _argsOption.ConnectionString,
                                    gitHubRepo: _argsOption.GitHubRepo,
                                    serverConnectionCount: _argsOption.ServerConnectionCount)).ToList();
            Task Process<T>(T p) => BatchProcess(packages, CreateWebApp, _argsOption.ConcurrentCountOfWebApp);
            bool NotReadyCheck<T>(T t) => isAnyWebAppNotReady(appNameList, _argsOption.GroupName);
            await RetriableRun(packages, Process, NotReadyCheck, 60, "create webapp");
        }

        private async Task ScaleOutAppPlan(List<string> appNameList)
        {
            var packages = (from i in Enumerable.Range(0, appNameList.Count)
                            where _azure.AppServices.AppServicePlans.GetByResourceGroup(_argsOption.GroupName, appNameList[i]) != null
                            select (azure: _azure,
                                    name: appNameList[i],
                                    groupName: _argsOption.GroupName,
                                    scaleOut: _scaleOut)).ToList();
            Task Process<T>(T p) => BatchProcess(packages, ScaleOutAppPlanAsync, _argsOption.ConcurrentCountOfServicePlan);
            bool NotReadyCheck<T>(T t) => isAnyServicePlanScaleOutNotReady(appNameList, _argsOption.GroupName);
            await RetriableRun(packages, Process, NotReadyCheck, 120, "scale out app plan", 5);
        }

        private async Task EnableWebAppLogs(List<string> appNameList)
        {
            var packages = (from i in Enumerable.Range(0, appNameList.Count)
                            where _azure.WebApps.GetByResourceGroup(_argsOption.GroupName, appNameList[i]) != null
                            select (azure: _azure,
                                    name: appNameList[i],
                                    groupName: _argsOption.GroupName)).ToList();
            Task Process<T>(T p) => BatchProcess(packages, WebAppEnableLog, _argsOption.ConcurrentCountOfWebApp);
            bool NotReadyCheck<T>(T t) => isDiagnosticLogNotReady(appNameList, _argsOption.GroupName);
            await RetriableRun(packages, Process, NotReadyCheck, 60, "enable diagnostic log", 5);
        }

        public async Task Deploy()
        {
            Login();
            if (_argsOption.RemoveResourceGroup == 1)
            {
                RemoveResourceGroup();
                return;
            }
            if (!ValidateDeployParameters())
            {
                return;
            }
            var sw = new Stopwatch();
            sw.Start();
            _resourceGroup = GetResourceGroup();
            // assign names
            var webappNameList = GenerateAppPlanNameList();
            if (!FindPricingTier(_argsOption.PriceTier, out PricingTier targetPricingTier))
            {
                Console.WriteLine($"Unsupported pricing tier: {_argsOption.PriceTier}");
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
            await EnableWebAppLogs(webappNameList);
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
