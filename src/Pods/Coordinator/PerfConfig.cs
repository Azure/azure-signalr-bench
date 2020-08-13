// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Coordinator
{
    public class PerfConfig
    {
        public static string PrefixPerf { get; private set; }

        public static string ResourceGroup
        {
            get
            {
                return PrefixPerf + "rg";
            }
        }

        public static string AKS
        {
            get
            {
                return PrefixPerf + "aks";
            }
        }

        public static AzureEnvironment AzureEnvironment;

        public static string Subscription { get; private set; }

        public static AzureCredentials ServicePrincipal { get; private set; }

        public static string KubeConfig { get; private set; }

        public static class Queue
        {
            public static string PortalJob = "portal-job";
        }

        public static class PPE
        {
            public static AzureEnvironment Cloud
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

            public static string Subscription { get; private set; }

            public static AzureCredentials ServicePrincipal { get; private set; }
        }

        public static void Init(SecretClient SecretClient)
        {
            Sp sp = null;
            var taskList = new List<Task>
            {
                Task.Run(async () => PrefixPerf = (await SecretClient.GetSecretAsync("prefix")).Value.Value),
                Task.Run(async () => Subscription = (await SecretClient.GetSecretAsync("subscription")).Value.Value),
                Task.Run(async () => sp = JsonConvert.DeserializeObject<Sp>((await SecretClient.GetSecretAsync("service-principal")).Value.Value)),
                Task.Run(async () =>
                {
                    string cloud = (await SecretClient.GetSecretAsync("cloud")).Value.Value;
                    cloud = cloud == "AzureCloud" ? "AzureGlobalCloud" : cloud;
                     AzureEnvironment =
                            AzureEnvironment.FromName(cloud);
                }),
                Task.Run(async () => KubeConfig = (await SecretClient.GetSecretAsync("kube-config")).Value.Value),
            };

            try
            {
                Task.WaitAll(taskList.ToArray());

                ServicePrincipal = SdkContext.AzureCredentialsFactory.FromServicePrincipal(
                    sp.appId,
                    sp.password,
                    sp.tenant, AzureEnvironment
                );
            }
            catch (Exception e)
            {
                Console.WriteLine("PerfInit error, exiting", e);
                Environment.Exit(1);
            }
            //init ppe
        }

        private class Sp
        {
            internal string appId;
            internal string name;
            internal string password;
            internal string tenant;
        }
    }
}
