using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DeployWebApp
{
    public class WebAppManagementBase
    {
        protected ArgsOption _argsOption;
        protected IAzure _azure;
        protected IDictionary<string, PricingTier> _priceTierMapper;
        protected PricingTier _targetPricingTier;
        protected IResourceGroup _resourceGroup;
        //protected int _webAppCount;
        protected int _appPlanCount;
        protected int _appInstanceCount;
        protected int _scaleOut;
        protected static int MAX_SCALE_OUT = 10;

        public WebAppManagementBase(ArgsOption argsOption)
        {
            _argsOption = argsOption;
            _priceTierMapper = new Dictionary<string, PricingTier>()
            {
                { "StandardS1", PricingTier.StandardS1},
                { "StandardS2", PricingTier.StandardS2},
                { "StandardS3", PricingTier.StandardS3},
                { "PremiumP1v2", PricingTier.PremiumP1v2},
                { "PremiumP2v2", PricingTier.PremiumP2v2}
            };
            _appInstanceCount = _argsOption.WebappCount;
            var grp = _argsOption.WebappCount / MAX_SCALE_OUT;
            var left = _argsOption.WebappCount % MAX_SCALE_OUT;
            if (grp == 0)
            {
                // less than 10 instance
                _appPlanCount = 1;
                _scaleOut = _appInstanceCount;
            }
            else if (left == 0)
            {
                _appPlanCount = grp;
                _scaleOut = MAX_SCALE_OUT;
            }
            else
            {
                if (_argsOption.WebappCount % 7 == 0)
                {
                    _appPlanCount = _argsOption.WebappCount / 7;
                    _scaleOut = 7;
                }
                else if (_argsOption.WebappCount % 5 == 0)
                {
                    _appPlanCount = _argsOption.WebappCount / 5;
                    _scaleOut = 5;
                }
                else if (_argsOption.WebappCount % 3 == 0)
                {
                    _appPlanCount = _argsOption.WebappCount / 3;
                    _scaleOut = 3;
                }
                else if (_argsOption.WebappCount % 2 == 0)
                {
                    _appPlanCount = _argsOption.WebappCount / 2;
                    _scaleOut = 2;
                }
                else
                {
                    throw new InvalidDataException("The web app instance count should be divided by 2 or 3 or 5 or 7");
                }
            }
        }

        protected List<string> GenerateAppPlanNameList()
        {
            var rootTimestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var webappNameList = new List<string>();
            for (var i = 0; i < _appPlanCount; i++)
            {
                var name = _argsOption.WebAppNamePrefix + $"{rootTimestamp}{i}";
                webappNameList.Add(name);
            }
            return webappNameList;
        }

        protected bool ValidateDeployParameters()
        {
            if (_argsOption.ConnectionString == null)
            {
                Console.WriteLine("No connection string is specified!");
                return false;
            }
            if (_argsOption.ServicePrincipal == null &&
                (_argsOption.ClientId == null ||
                _argsOption.ClientSecret == null ||
                _argsOption.SubscriptionId == null ||
                _argsOption.TenantId == null))
            {
                Console.WriteLine("No secret or credential is specified!");
            }
            return true;
        }

        protected IResourceGroup GetResourceGroup()
        {
            IResourceGroup resourceGroup = null;
            if (_azure.ResourceGroups.Contain(_argsOption.GroupName))
            {
                if (_argsOption.RemoveExistingResourceGroup == 1)
                {
                    RemoveResourceGroup();
                    resourceGroup = _azure.ResourceGroups.Define(_argsOption.GroupName)
                                     .WithRegion(_argsOption.Location)
                                     .Create();
                }
                else
                {
                    resourceGroup = _azure.ResourceGroups.GetByName(_argsOption.GroupName);
                }
            }
            else
            {
                resourceGroup = _azure.ResourceGroups.Define(_argsOption.GroupName)
                                     .WithRegion(_argsOption.Location)
                                     .Create();
            }
            return resourceGroup;
        }

        protected void Login()
        {
            AzureCredentials credentials = null;
            if (_argsOption.ServicePrincipal == null)
            {
                credentials = SdkContext.AzureCredentialsFactory
                    .FromServicePrincipal(_argsOption.ClientId,
                    _argsOption.ClientSecret,
                    _argsOption.TenantId,
                    AzureEnvironment.AzureGlobalCloud);
            }
            else
            {
                var configLoader = new ConfigurationLoader();
                var sp = configLoader.Load<ServicePrincipalConfig>(_argsOption.ServicePrincipal);
                credentials = SdkContext.AzureCredentialsFactory
                    .FromServicePrincipal(sp.ClientId,
                    sp.ClientSecret,
                    sp.TenantId,
                    AzureEnvironment.AzureGlobalCloud);
                _argsOption.SubscriptionId = sp.Subscription;
            }

            _azure = Azure
                .Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(credentials)
                .WithSubscription(_argsOption.SubscriptionId);
        }

        protected void RemoveResourceGroup()
        {
            var maxRetry = 3;
            var i = 0;
            while (i < maxRetry)
            {
                try
                {
                    if (_azure.ResourceGroups.Contain(_argsOption.GroupName))
                    {
                        _azure.ResourceGroups.DeleteByName(_argsOption.GroupName);
                    }
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error occur: {e.Message}");
                }
                i++;
            }
        }

        protected bool FindPricingTier(string priceTierValue, out PricingTier result)
        {
            var found = _priceTierMapper.TryGetValue(priceTierValue, out PricingTier v);
            result = v;
            return found;
        }

        protected void DumpAppServicePlanId(List<string> webappNameList)
        {
            string appServicePlanIdList = "";

            for (var i = 0; i < _appPlanCount; i++)
            {
                var name = webappNameList[i];
                var appPlan = _azure.AppServices.AppServicePlans.GetByResourceGroup(_argsOption.GroupName, name);
                if (appPlan != null)
                {
                    var id = appPlan.Id;
                    appServicePlanIdList += id + Environment.NewLine;
                }
            }
            if (_argsOption.AppServicePlanIdOutputFile != null)
            {
                if (File.Exists(_argsOption.AppServicePlanIdOutputFile))
                {
                    File.Delete(_argsOption.AppServicePlanIdOutputFile);
                }
                using (var writer = new StreamWriter(_argsOption.AppServicePlanIdOutputFile, true))
                {
                    writer.WriteLine(appServicePlanIdList);
                }
            }
            else
            {
                Console.WriteLine(appServicePlanIdList);
            }
        }

        protected void DumpWebAppId(List<string> webappNameList)
        {
            string webappIdList = "";

            for (var i = 0; i < _appPlanCount; i++)
            {
                var name = webappNameList[i];
                var webApp = _azure.WebApps.GetByResourceGroup(_argsOption.GroupName, name);
                if (webApp != null)
                {
                    var id = webApp.Id;
                    webappIdList += id + Environment.NewLine;
                }
            }
            if (_argsOption.WebAppIdOutputFile != null)
            {
                if (File.Exists(_argsOption.WebAppIdOutputFile))
                {
                    File.Delete(_argsOption.WebAppIdOutputFile);
                }
                using (var writer = new StreamWriter(_argsOption.WebAppIdOutputFile, true))
                {
                    writer.WriteLine(webappIdList);
                }
            }
            else
            {
                Console.WriteLine(webappIdList);
            }
        }

        protected void DumpWebAppUrl(List<string> webappNameList)
        {
            if (_argsOption.OutputFile == null)
            {
                for (var i = 0; i < _appPlanCount; i++)
                {
                    var webApp = _azure.WebApps.GetByResourceGroup(_argsOption.GroupName, webappNameList[i]);
                    if (webApp != null)
                    {
                        Console.WriteLine($"https://{webappNameList[i]}.azurewebsites.net");
                    }
                }
            }
            else
            {
                if (File.Exists(_argsOption.OutputFile))
                {
                    File.Delete(_argsOption.OutputFile);
                }

                using (var writer = new StreamWriter(_argsOption.OutputFile, true))
                {
                    string result = "";
                    for (var i = 0; i < _appPlanCount; i++)
                    {
                        var webApp = _azure.WebApps.GetByResourceGroup(_argsOption.GroupName, webappNameList[i]);
                        if (webApp != null)
                        {
                            if (result.Length == 0)
                                result = $"https://{webappNameList[i]}.azurewebsites.net/{_argsOption.HubName}";
                            else
                                result = result + "," + $"https://{webappNameList[i]}.azurewebsites.net/{_argsOption.HubName}";
                        }
                    }
                    writer.WriteLine(result);
                }
            }
        }

        protected bool isAllServicePlanCreated(List<string> names, string resourceGroup)
        {
            foreach (var n in names)
            {
                var iAppPlan = _azure.AppServices.AppServicePlans.GetByResourceGroup(resourceGroup, n);
                if (iAppPlan == null)
                {
                    return false;
                }
            }
            return true;
        }

        protected bool isAllWebAppCreated(List<string> names, string resourceGroup)
        {
            foreach (var n in names)
            {
                var webApp = _azure.WebApps.GetByResourceGroup(resourceGroup, n);
                if (webApp == null)
                {
                    return false;
                }
            }
            return true;
        }

        protected static async Task CreateAppPlanCoreAsync(
            (IAzure azure,
            string name,
            string region,
            string groupName,
            PricingTier pricingTier,
            int scaleOut,
            Microsoft.Azure.Management.AppService.Fluent.OperatingSystem os) package)
        {
            var funcName = "CreateAppPlanCoreAsync";
            using (var cts = new CancellationTokenSource(TimeSpan.FromHours(1)))
            {
                var iAppPlan = package.azure.AppServices.AppServicePlans.GetByResourceGroup(package.groupName, package.name);
                if (iAppPlan == null)
                {
                    await package.azure.AppServices.AppServicePlans
                            .Define(package.name)
                            .WithRegion(package.region)
                            .WithExistingResourceGroup(package.groupName)
                            .WithPricingTier(package.pricingTier)
                            .WithOperatingSystem(package.os)
                            .WithCapacity(package.scaleOut)
                            .CreateAsync(cts.Token);
                    Console.WriteLine($"{DateTime.Now.ToString("yyyyMMddHHmmss")} Successfully {funcName} for {package.name}");
                }
                else
                {
                    Console.WriteLine($"{DateTime.Now.ToString("yyyyMMddHHmmss")} {funcName} for {package.name} already existed");
                }
            }
        }

        protected static async Task CreateWebAppCoreAsync(
            (IAzure azure,
            string name,
            IAppServicePlan appServicePlan,
            IResourceGroup resourceGroup,
            string connectionString,
            string githubRepo) package)
        {
            var funcName = "CreateWebAppCoreAsync";
            using (var cts = new CancellationTokenSource(TimeSpan.FromHours(1)))
            {
                var webapp = package.azure.WebApps.GetByResourceGroup(package.resourceGroup.Name, package.name);
                if (webapp == null)
                {
                    if (package.appServicePlan != null)
                    {
                        await package.azure.WebApps.Define(package.name)
                                                   .WithExistingWindowsPlan(package.appServicePlan)
                                                   .WithExistingResourceGroup(package.resourceGroup)
                                                   .WithWebSocketsEnabled(true)
                                                   .WithWebAppAlwaysOn(true)
                                                   .DefineSourceControl()
                                                   .WithPublicGitRepository(package.githubRepo)
                                                   .WithBranch("master")
                                                   .Attach()
                                                   .WithConnectionString("Azure:SignalR:ConnectionString", package.connectionString,
                                                    Microsoft.Azure.Management.AppService.Fluent.Models.ConnectionStringType.Custom)
                                                   .CreateAsync(cts.Token);
                        Console.WriteLine($"{DateTime.Now.ToString("yyyyMMddHHmmss")} Successfully {funcName} for {package.name}");
                    }
                    else
                    {
                        Console.WriteLine($"{DateTime.Now.ToString("yyyyMMddHHmmss")} Fail to create {funcName} for {package.name} because app plan is not available");
                    }
                }
                else
                {
                    Console.WriteLine($"{DateTime.Now.ToString("yyyyMMddHHmmss")} {funcName} for {package.name} already existed");
                }
            }
        }


        protected static Task BatchProcess<T>(IList<T> source, Func<T, Task> f, int max)
        {
            var initial = (max >> 1);
            var s = new System.Threading.SemaphoreSlim(initial, max);
            _ = Task.Run(async () =>
            {
                for (int i = initial; i < max; i++)
                {
                    await Task.Delay(100);
                    s.Release();
                }
            });

            return Task.WhenAll(from item in source
                                select Task.Run(async () =>
                                {
                                    await s.WaitAsync();
                                    try
                                    {
                                        await f(item);
                                    }
                                    finally
                                    {
                                        s.Release();
                                    }
                                }));
        }
    }
}
