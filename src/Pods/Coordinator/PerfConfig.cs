using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Coordinator
{
    class PerfConfig
    {
        public static string PREFIX_PERF { get; private set; }

        public static string RESOUCE_GROUP
        {
            get
            {
                return PREFIX_PERF + "rg";
            }
        }

        public static string AKS
        {
            get
            {
                return PREFIX_PERF + "aks";
            }
        }

        public static AzureEnvironment AZURE_ENVIRONMENT;

        public static string SUBSCRIPTION { get; private set; }

        public static AzureCredentials SERVICE_PRINCIPAL { get; private set; }

        public static string KUBE_CONFIG { get; private set; }

        public static class Queue
        {
            public static string PORTAL_JOB = "portal-job";
        }

        public static class PPE
        {
            public static AzureEnvironment CLOUD
            {
                get
                {
                    return new AzureEnvironment
                    {
                        GraphEndpoint = "https://graph.ppe.windows.net/",
                        AuthenticationEndpoint = "https://login.windows-ppe.net",
                        Name = "PPE",
                        ManagementEndpoint = "https://umapi-preview.core.windows-int.net/",
                        ResourceManagerEndpoint = "https://api-dogfood.resources.windows-int.net/"
                    };
                }
            }

            public static string SUBSCRIPTION { get; private set; }

            public static AzureCredentials SERVICE_PRINCIPAL { get; private set; }
        }

        public static void Init(SecretClient SecretClient)
        {
            SP sp = null;
            var taskList = new List<Task>
            {
                Task.Run(async () => PREFIX_PERF = (await SecretClient.GetSecretAsync("prefix")).Value.Value),
                Task.Run(async () => SUBSCRIPTION = (await SecretClient.GetSecretAsync("subscription")).Value.Value),
                Task.Run(async () => sp = JsonConvert.DeserializeObject<SP>((await SecretClient.GetSecretAsync("service-principal")).Value.Value)),
                Task.Run(async () =>
                {
                    string cloud = (await SecretClient.GetSecretAsync("cloud")).Value.Value;
                    cloud = cloud == "AzureCloud" ? "AzureGlobalCloud" : cloud;
                     AZURE_ENVIRONMENT =
                            AzureEnvironment.FromName(cloud);
                }),
                Task.Run(async () => KUBE_CONFIG = (await SecretClient.GetSecretAsync("kube-config")).Value.Value),
            };

            try
            {
                Task.WaitAll(taskList.ToArray());

                SERVICE_PRINCIPAL = SdkContext.AzureCredentialsFactory.FromServicePrincipal(
                    sp.appId,
                    sp.password,
                    sp.tenant, AZURE_ENVIRONMENT
                );
            }
            catch (Exception e)
            {
                Environment.Exit(1);
            }
            //init ppe
        }

        private class SP
        {
            public string appId;
            public string name;
            public string password;
            public string tenant;
        }
    }
}
