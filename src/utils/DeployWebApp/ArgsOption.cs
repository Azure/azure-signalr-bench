using CommandLine;

namespace DeployWebApp
{
    public class CommonArgsOption
    {
        [Option("clientId", Required = false, HelpText = "Azure client ID")]
        public string ClientId { get; set; }

        [Option("clientSecret", Required = false, HelpText = "Azure client secret")]
        public string ClientSecret { get; set; }

        [Option("tenantId", Required = false, HelpText = "Azure tenant Id")]
        public string TenantId { get; set; }

        [Option("subscriptionId", Required = false, HelpText = "Azure tenant Id")]
        public string SubscriptionId { get; set; }

        [Option("servicePrincipal", Required = false, HelpText = "Specify service principal file")]
        public string ServicePrincipal { get; set; }

        [Option("resourceGroup", Required = false, Default = "aspnetSignalR2", HelpText = "Azure resource group name")]
        public string GroupName { get; set; }

        [Option("appServicePlanIdOutputFile", Required = false,
            HelpText = "Specify the app service plan output file, default is null and output to console")]
        public string AppServicePlanIdOutputFile { get; set; }

        [Option("webAppIdOutputFile", Required = false,
            HelpText = "Specify the web app output file, default is null and output to console")]
        public string WebAppIdOutputFile { get; set; }

        [Option("appServicePlanScaleOutputFile", Required = false,
            HelpText = "Specify the output file for app service plan scale out count, default is null and output to console")]
        public string AppServicePlanScaleOutputFile { get; set; }

        [Option("outputFile", Required = false, HelpText = "Specify the output file, default is null and output to console")]
        public string OutputFile { get; set; }

        [Option("hubName", Required = false, Default = "signalrbench", HelpText = "Specify the hubName which will show up in the end of URL, only for benchmark use")]
        public string HubName { get; set; }
    }

    [Verb("deploy", HelpText = "Deploy web app on Azure, use 'help deploy' to find more options")]
    public class DeployOption : CommonArgsOption
    {
        [Option("location", Required = false, Default = "southeastasia", HelpText = "Azure region")]
        public string Location { get; set; }

        [Option("webappNamePrefix", Required = false, Default = "signalrwebapp", HelpText = "Specify web app name")]
        public string WebAppNamePrefix { get; set; }

        [Option("webappCount", Required = false, Default = 2, HelpText = "Webapp instance count")]
        public int WebappCount { get; set; }

        [Option("removeExistingResourceGroup", Required = false, Default = 0, HelpText = "Remove existing resource group")]
        public int RemoveExistingResourceGroup { get; set; }

        [Option("githubRepo", Required = false, Default = "https://github.com/clovertrail/AspNetServer", HelpText = "github repo")]
        public string GitHubRepo { get; set; }

        [Option("connectionString", Required = false, Default = null, HelpText = "ASRS connection string")]
        public string ConnectionString { get; set; }

        [Option("priceTier", Required = false, Default = "StandardS3", HelpText = "Specify the price tier you want to run <StandardS1|StandardS2|StandardS3|PremiumP1v2|PremiumP1v2>, default is StandardS3")]
        public string PriceTier { get; set; }

        [Option("appPlanName", Required = false, Default = "myAppPlan", HelpText = "Specify the app plan name you want to query")]
        public string AppPlanName { get; set; }

        [Option("serverConnectionCount", Required = false, Default = "15", HelpText = "Specify the server connection count")]
        public string ServerConnectionCount { get; set; }

        [Option("concurrentCountOfServicePlan", Required = false, Default = 1, HelpText = "Concurrent count of creating service plan")]
        public int ConcurrentCountOfServicePlan { get; set; }

        [Option("concurrentCountOfWebApp", Required = false, Default = 2, HelpText = "Concurrent count of creating webapp")]
        public int ConcurrentCountOfWebApp { get; set; }
    }

    [Verb("getInfo", HelpText = "Get information for the specific resource, use 'help getInfo' to find more options")]
    public class GetInfoOption : CommonArgsOption
    {
        [Option("appPlanName", Required = false, Default = "myAppPlan", HelpText = "Specify the app plan name you want to query")]
        public string AppPlanName { get; set; }
    }

    [Verb("downloadLog", HelpText = "Download the log, use 'help downloadLog' to find more options")]
    public class DownloadLogOption : CommonArgsOption
    {
        [Option("WebAppResourceId", Required = false, HelpText = "query webapp information by its resourceId")]
        public string WebAppResourceId { get; set; }

        [Option("LogPrefix", Required = false, Default = "app", HelpText = "Sepcify the log filename prefix, default is 'app'")]
        public string LogPrefix { get; set; }

        [Option("LogPostfix", Required = false, Default = "log", HelpText = "Sepcify the log filename postfix, default is 'log'")]
        public string LogPostfix { get; set; }

        [Option("LocalFilePrefix", Required = false, Default = "appserver", HelpText = "Sepcify the local log file, default is 'appserver'")]
        public string LocalLogFilePrefix { get; set; }
    }

    [Verb("removeGroup", HelpText = "Remove resource group, use 'help removeGroup' to find more options")]
    public class RemoveGroupOption : CommonArgsOption
    {

    }
}
