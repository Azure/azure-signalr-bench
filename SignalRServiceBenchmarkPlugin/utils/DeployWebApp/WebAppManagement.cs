using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
            var credentials = SdkContext.AzureCredentialsFactory
                    .FromServicePrincipal(_argsOption.ClientId,
                    _argsOption.ClientSecret,
                    _argsOption.TenantId,
                    AzureEnvironment.AzureGlobalCloud);

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

        public async Task Deploy()
        {
            Login();
            if (_argsOption.RemoveResourceGroup == 1)
            {
                RemoveResourceGroup();
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
            var planTaskList = new List<Task<IAppServicePlan>>();
            for (var i = 0; i < _argsOption.WebappCount; i++)
            {
                var appServicePlan = _azure.AppServices.AppServicePlans
                                    .Define(webappNameList[i])
                                    .WithRegion(_argsOption.Location)
                                    .WithExistingResourceGroup(_argsOption.GroupName)
                                    .WithPricingTier(PricingTier.PremiumP1v2)
                                    .WithOperatingSystem(Microsoft.Azure.Management.AppService.Fluent.OperatingSystem.Windows)
                                    .CreateAsync();
                planTaskList.Add(appServicePlan);
            }

            await Task.WhenAll(planTaskList);

            // create webapp
            var servicePlanList = new List<IAppServicePlan>();
            foreach (var t in planTaskList)
            {
                servicePlanList.Add(t.GetAwaiter().GetResult());
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
