using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using System;
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

        protected static async Task CreateAppPlan(
            (IAzure azure,
            string name,
            string region,
            string groupName,
            PricingTier pricingTier,
            int scaleOut,
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
            var retry = 0;
            var maxRetry = 3;
            // create app service plans
            do
            {
                Console.WriteLine($"{DateTime.Now.ToString("yyyyMMddHHmmss")} {retry} : create app plan");
                var packages = (from i in Enumerable.Range(0, webappNameList.Count)
                                where _azure.AppServices.AppServicePlans.GetByResourceGroup(groupName, webappNameList[i]) == null
                                select (azure: _azure,
                                        name: webappNameList[i],
                                        region: _argsOption.Location,
                                        groupName: _argsOption.GroupName,
                                        pricingTier: _targetPricingTier,
                                        scaleOut: _scaleOut,
                                        os: Microsoft.Azure.Management.AppService.Fluent.OperatingSystem.Windows)).ToList();
                await BatchProcess(packages, CreateAppPlan, _argsOption.ConcurrentCountOfServicePlan);
                if (retry > 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(60));
                }
                retry++;
            } while (!isAllServicePlanCreated(webappNameList, groupName) && retry < maxRetry);

            sw.Stop();
            Console.WriteLine($"it takes {sw.ElapsedMilliseconds} ms to create app plan");

            sw.Start();
            // create webapp
            retry = 0;
            do
            {
                var packages = (from i in Enumerable.Range(0, webappNameList.Count)
                                 where _azure.WebApps.GetByResourceGroup(groupName, webappNameList[i]) == null
                                 select (azure: _azure,
                                         name: webappNameList[i],
                                         appServer: _azure.AppServices.AppServicePlans.GetByResourceGroup(_argsOption.GroupName, webappNameList[i]),
                                         resourceGroup: _resourceGroup,
                                         connectionString: _argsOption.ConnectionString,
                                         gitHubRepo: _argsOption.GitHubRepo,
                                         serverConnectionCount: _argsOption.ServerConnectionCount)).ToList();
                await BatchProcess(packages, CreateWebApp, _argsOption.ConcurrentCountOfWebApp);
                if (retry > 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
                retry++;
            } while (!isAllWebAppCreated(webappNameList, groupName) && retry < maxRetry);

            sw.Stop();
            Console.WriteLine($"it takes {sw.ElapsedMilliseconds} ms");

            // output app service plan Id
            DumpAppServicePlanId(webappNameList);
            // output web app Id
            DumpWebAppId(webappNameList);
            // dump results
            DumpWebAppUrl(webappNameList);
        }
    }
}
