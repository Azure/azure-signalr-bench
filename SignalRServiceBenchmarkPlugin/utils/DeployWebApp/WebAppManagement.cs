using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DeployWebApp
{
    public class WebAppManagement
    {
        private ArgsOption _argsOption;
        private IAzure _azure;

        private IResourceGroup GetResourceGroup()
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

        private void Login()
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


        private void RemoveResourceGroup()
        {
            _azure.ResourceGroups.DeleteByName(_argsOption.GroupName);
        }

        public WebAppManagement(ArgsOption argsOption)
        {
            _argsOption = argsOption;
        }

        private bool ValidateDeployParameters()
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

        private static Task BatchProcess<T>(IList<T> source, Func<T, Task> f, int max)
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

        public static Task CreateAppPlan(
            (IAzure azure,
            string name,
            string region,
            string groupName,
            PricingTier pricingTier,
            Microsoft.Azure.Management.AppService.Fluent.OperatingSystem os) package)
        {
            return package.azure.AppServices.AppServicePlans
                                    .Define(package.name)
                                    .WithRegion(package.region)
                                    .WithExistingResourceGroup(package.groupName)
                                    .WithPricingTier(package.pricingTier)
                                    .WithOperatingSystem(package.os)
                                    .CreateAsync();
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
            IResourceGroup resourceGroup = GetResourceGroup();
            // assign names
            var rootTimestamp = DateTime.Now.ToString("yyyyMMddHH");
            var webappNameList = new List<string>();
            for (var i = 0; i < _argsOption.WebappCount; i++)
            {
                var name = _argsOption.WebAppNamePrefix + $"{rootTimestamp}{i}";
                webappNameList.Add(name);
            }
            // create app service plans
            var packages = (from i in Enumerable.Range(0, _argsOption.WebappCount)
                            select (azure : _azure,
                                    name : webappNameList[i],
                                    region : _argsOption.Location,
                                    groupName : _argsOption.GroupName,
                                    pricingTier: PricingTier.PremiumP1v2,
                                    os : Microsoft.Azure.Management.AppService.Fluent.OperatingSystem.Windows)).ToList();

            await BatchProcess(packages, CreateAppPlan, _argsOption.ConcurrentCountOfServicePlan);

            // create webapp
            var servicePlanList = new List<IAppServicePlan>();
            for (var i = 0; i < _argsOption.WebappCount; i++)
            {
                var appService = _azure.AppServices.AppServicePlans.GetByResourceGroup(_argsOption.GroupName, webappNameList[i]);
                servicePlanList.Add(appService);
            }
            var tasks = new List<Task>();
            for (var i = 0; i < _argsOption.WebappCount; i++)
            {
                var name = webappNameList[i];
                var t = _azure.WebApps.Define(name)
                         .WithExistingWindowsPlan(servicePlanList[i])
                         .WithExistingResourceGroup(resourceGroup)
                         .WithWebAppAlwaysOn(true)
                         .DefineSourceControl()
                         .WithPublicGitRepository(_argsOption.GitHubRepo)
                         .WithBranch("master")
                         .Attach()
                         .WithConnectionString("Azure:SignalR:ConnectionString", _argsOption.ConnectionString,
                         Microsoft.Azure.Management.AppService.Fluent.Models.ConnectionStringType.Custom)
                         .CreateAsync();
                tasks.Add(t);
            }
            await Task.WhenAll(tasks);
            sw.Stop();
            Console.WriteLine($"it takes {sw.ElapsedMilliseconds} ms");
            // dump results
            if (_argsOption.OutputFile == null)
            {
                for (var i = 0; i < _argsOption.WebappCount; i++)
                {
                    Console.WriteLine($"https://{webappNameList[i]}.azurewebsites.net");
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
                    for (var i = 0; i < _argsOption.WebappCount; i++)
                    {
                        if (i == 0)
                            result = $"https://{webappNameList[i]}.azurewebsites.net/{_argsOption.HubName}";
                        else
                            result = result + $"https://{webappNameList[i]}.azurewebsites.net/{_argsOption.HubName}";
                        if (i + 1 < _argsOption.WebappCount)
                        {
                            result = result + ",";
                        }
                    }
                    writer.WriteLine(result);
                }
            }
        }

    }
}
