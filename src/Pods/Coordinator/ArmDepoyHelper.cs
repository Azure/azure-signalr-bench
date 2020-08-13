// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using MAzure = Microsoft.Azure.Management.Fluent.Azure;

namespace Coordinator
{
    internal class ArmDeployHelper
    {
        public IAzure GetAzure(AzureEnvironment azureEnvironment)
        {
            if (azureEnvironment.Name == "PPE")
            {
                return MAzure.Configure().Authenticate(PerfConfig.PPE.ServicePrincipal).WithSubscription(PerfConfig.PPE.Subscription);
            }
            else
            {
                return MAzure.Configure().Authenticate(PerfConfig.ServicePrincipal).WithSubscription(PerfConfig.Subscription);
            }
        }

        public async Task Deploy(string deploymentName, IAzure azure, JObject deployTemplate, JObject deployParams)
        {
            await azure.Deployments.Define(deploymentName).WithExistingResourceGroup(PerfConfig.ResourceGroup).WithTemplate(deployTemplate.ToString()).WithParameters(deployParams).WithMode(DeploymentMode.Incremental).BeginCreateAsync();
        }
    }
}
