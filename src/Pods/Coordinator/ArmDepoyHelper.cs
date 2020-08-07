using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace Coordinator
{
    class ArmDeployHelper
    {
        public IAzure GetAzure(AzureEnvironment azureEnvironment)
        {
            if (azureEnvironment.Name == "PPE")
            {
                return Microsoft.Azure.Management.Fluent.Azure.Configure().Authenticate(PerfConfig.PPE.SERVICE_PRINCIPAL).WithSubscription(PerfConfig.PPE.SUBSCRIPTION);
            }
            else
            {
                return Microsoft.Azure.Management.Fluent.Azure.Configure().Authenticate(PerfConfig.SERVICE_PRINCIPAL).WithSubscription(PerfConfig.SUBSCRIPTION);
            }
        }
        public async Task Deploy(string deploymentName, IAzure azure, JObject deployTemplate, JObject deployParams)
        {
            await azure.Deployments.Define(deploymentName).WithExistingResourceGroup(PerfConfig.RESOUCE_GROUP).WithTemplate(deployTemplate.ToString()).WithParameters(deployParams).WithMode(Microsoft.Azure.Management.ResourceManager.Fluent.Models.DeploymentMode.Incremental).BeginCreateAsync();
        }
    }
}
