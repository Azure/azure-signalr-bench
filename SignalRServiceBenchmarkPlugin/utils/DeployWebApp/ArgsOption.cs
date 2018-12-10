using CommandLine;

namespace DeployWebApp
{
    public class ArgsOption
    {
        [Option("clientId", Required = true, HelpText = "Azure client ID")]
        public string ClientId { get; set; }

        [Option("clientSecret", Required = true, HelpText = "Azure client secret")]
        public string ClientSecret { get; set; }

        [Option("tenantId", Required = true, HelpText = "Azure tenant Id")]
        public string TenantId { get; set; }

        [Option("subscriptionId", Required = true, HelpText = "Azure tenant Id")]
        public string SubscriptionId { get; set; }

        [Option("resourceGroup", Required = false, Default = "aspnetSignalR2", HelpText = "Azure resource group name")]
        public string GroupName { get; set; }

        [Option("removeResourceGroup", Required = false, Default = 0, HelpText = "Only remove existing resource group")]
        public int RemoveResourceGroup { get; set; }

        [Option("location", Required = true, HelpText = "Azure region")]
        public string Location { get; set; }

        [Option("webappNamePrefix", Required = false, Default = "signalrwebapp", HelpText = "Specify web app name")]
        public string WebAppNamePrefix { get; set; }

        [Option("webappCount", Required = true, Default = 1, HelpText = "Webapp count")]
        public int WebappCount { get; set; }

        [Option("removeExistingResourceGroup", Required = false, Default = 0, HelpText = "Remove existing resource group")]
        public int RemoveExistingResourceGroup { get; set; }

        [Option("githubRepo", Required = false, Default = "https://github.com/clovertrail/AspNetServer", HelpText = "github repo")]
        public string GitHubRepo { get; set; }

        [Option("connectionString", Required = true, HelpText = "ASRS connection string")]
        public string ConnectionString { get; set; }

        [Option("outputFile", Required = false, HelpText = "Specify the output file, default is null and output to console")]
        public string OutputFile { get; set; }

        [Option("hubName", Required = false, Default = "signalrbench", HelpText = "Specify the hubName which will show up in the end of URL, only for benchmark use")]
        public string HubName { get; set; }
    }
}
