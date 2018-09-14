using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

namespace JenkinsScript
{
    class AzureManager
    {

        private IAzure _azure;

        public AzureManager(string servicePrincipal)
        {
            try
            {
                LoginAzure(servicePrincipal);
            }
            catch (Exception ex)
            {
                Util.Log($"Login Azure Exception: {ex}");
            }
        }

        public void LoginAzure(string servicePrincipal)
        {
            var configLoader = new ConfigLoader();
            var sp = configLoader.Load<ServicePrincipalConfig>(servicePrincipal);

            // var content = AzureBlobReader.ReadBlob("ServicePrincipalFileName");
            // var sp = AzureBlobReader.ParseYaml<ServicePrincipalConfig>(content);

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
            else
            {
                Util.Log($"Resource group {name} doesn't exist");
            }
        }
    }
}