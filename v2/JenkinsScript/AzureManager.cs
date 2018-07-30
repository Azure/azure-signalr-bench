using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace JenkinsScript
{
    class AzureManager
    {

        private IAzure _azure;

        public AzureManager()
        {
            try
            {
                LoginAzure();
            }
            catch (Exception ex)
            {
                Util.Log($"Login Azure Exception: {ex}");
            }
        }

        public void LoginAzure()
        {
            var content = AzureBlobReader.ReadBlob("ServicePrincipalFileName");
            var sp = AzureBlobReader.ParseYaml<ServicePrincipalConfig>(content);

            // auth
            var credentials = SdkContext.AzureCredentialsFactory
                .FromServicePrincipal(sp.ClientId, sp.ClientSecret, sp.TenantId, AzureEnvironment.AzureGlobalCloud);

            _azure = Azure
                .Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(credentials)
                .WithSubscription(sp.Subscription);
        }

        public void DeleteResourceGroup(string name)
        {
            if (_azure.ResourceGroups.Contain(name))
            {
                _azure.ResourceGroups.DeleteByName(name);
            }
        }
    }
}
