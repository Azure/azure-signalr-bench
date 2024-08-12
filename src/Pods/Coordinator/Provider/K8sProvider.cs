// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Coordinator.Entities;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Configuration;

namespace Azure.SignalRBench.Coordinator.Provider
{
    public class K8SProvider : IK8sProvider
    {
        private const string Default = "default";
        private const string Appserver = "appserver";
        private const string Client = "client";
        private const string Upstream = "upstream";
        private readonly string _domain;
        private readonly PerfStorageProvider _perfStorageProvider;
        private readonly string _redisConnectionString;
        private readonly string _image;
        private Kubernetes? _k8S;
        private readonly bool _internal;

        public K8SProvider(PerfStorageProvider perfStorageProvider, IConfiguration configuration)
        {
            _perfStorageProvider = perfStorageProvider;
            _redisConnectionString = configuration[PerfConstants.ConfigurationKeys.RedisConnectionStringKey];
            _domain = configuration[PerfConstants.ConfigurationKeys.DomainKey];
            _image = configuration[PerfConstants.ConfigurationKeys.Image];
            _internal = bool.Parse(configuration[PerfConstants.ConfigurationKeys.Internal]);
            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            _k8S = new Kubernetes(KubernetesClientConfiguration.BuildDefaultConfig(), new HttpClient(handler));
        }

        public async Task<string> CreateServerPodsAsync(string testId, string[] asrsConnectionStrings,
            int serverPodCount, TestCategory testCategory, string formatProtocol, int perPodConnection,
            ClientBehavior behavior, CancellationToken cancellationToken)
        {
            var name = NameConverter.Truncate(testId);
            var service = new V1Service
            {
                Metadata = new V1ObjectMeta
                {
                    Name = name
                },
                Spec = new V1ServiceSpec
                {
                    Ports = new List<V1ServicePort>
                    {
                        new V1ServicePort(80, targetPort: 8080)
                    },
                    Selector = new Dictionary<string, string>
                    {
                        ["app"] = name,
                        ["type"] = Appserver
                    }
                }
            };
            await _k8S.CreateNamespacedServiceAsync(service, Default, cancellationToken: cancellationToken);
            if (testCategory == TestCategory.AspnetCoreSignalRServerless || (testCategory == TestCategory.RawWebsocket))
            {
                var ingress = new V1Ingress
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = NameConverter.Truncate(testId),
                        Annotations = new Dictionary<string, string>
                        {
                            ["kubernetes.io/ingress.class"] = "nginx",
                        }
                    },
                    Spec = new V1IngressSpec()
                    {
                        Rules = new List<V1IngressRule>
                        {
                            new V1IngressRule
                            {
                                Host = _domain,
                                Http = new V1HTTPIngressRuleValue()
                                {
                                    Paths = new List<V1HTTPIngressPath>
                                    {
                                        new V1HTTPIngressPath
                                        {
                                            PathType = "Prefix",
                                            Path = $"/upstream/{NameConverter.GenerateHubName(testId)}",
                                            Backend = new V1IngressBackend()
                                            {
                                                Service = new V1IngressServiceBackend()
                                                {
                                                    Name = name,
                                                    Port = new V1ServiceBackendPort()
                                                    {
                                                        Number = 80
                                                    }
                                                },
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                };
                await _k8S.CreateNamespacedIngress1Async(ingress, Default, cancellationToken: cancellationToken);
            }

            var server = testCategory switch
            {
                TestCategory.AspnetCoreSignalRServerless => "SignalRUpstream",
                TestCategory.AspnetSignalR => "AspNetAppServer",
                TestCategory.RawWebsocket => "WpsUpstream",
                TestCategory.SocketIO => "SioServer",
                TestCategory.WebPubSubPythonSdk => "WpsPyServer",
                TestCategory.Mqtt => "WpsUpstream",
                _ => "AppServer"
            };

            V1Deployment deployment = new V1Deployment
            {
                Metadata = new V1ObjectMeta
                {
                    Name = name,
                    Labels = new Dictionary<string, string>
                    {
                        //[PerfConstants.ConfigurationKeys.TestIdKey] = testId
                        ["type"] = Appserver
                    },
                    Annotations = new Dictionary<string, string>
                    {
                        ["cluster-autoscaler.kubernetes.io/safe-to-evict"] = "false"
                    }
                },
                Spec = new V1DeploymentSpec
                {
                    Replicas = serverPodCount,
                    Selector = new V1LabelSelector
                    {
                        MatchLabels = new Dictionary<string, string>
                        {
                            ["app"]=name,
                            ["type"] = Appserver
                        }
                    },
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta
                        {
                            CreationTimestamp = null,
                            Labels = new Dictionary<string, string>
                            {
                                ["app"] = name,
                                ["type"] = Appserver
                            }
                        },
                        Spec = new V1PodSpec
                        {
                            DnsConfig = new V1PodDNSConfig
                            {
                                Options = new List<V1PodDNSConfigOption>
                                {
                                    new V1PodDNSConfigOption
                                    {
                                        Name = "ndots",
                                        Value = "2"
                                    }
                                }
                            },
                            NodeSelector = new Dictionary<string, string>
                            {
                                [PerfConstants.Name.OsLabel] = testCategory == TestCategory.AspnetSignalR
                                    ? PerfConstants.Name.Windows
                                    : PerfConstants.Name.Linux
                            },
                            Containers = new List<V1Container>
                            {
                                new V1Container
                                {
                                    Name = name,
                                    Image = testCategory == TestCategory.AspnetSignalR
                                        ? "mcr.microsoft.com/dotnet/framework/runtime:4.8"
                                        : _image,
                                    Resources = new V1ResourceRequirements
                                    {
                                        Requests = new Dictionary<string, ResourceQuantity>
                                        {
                                            ["cpu"] = new ResourceQuantity("2500m"),
                                            ["memory"] = new ResourceQuantity("10000Mi")
                                        },
                                        Limits = new Dictionary<string, ResourceQuantity>
                                        {
                                            ["cpu"] = new ResourceQuantity("4000m"),
                                            ["memory"] = new ResourceQuantity("14000Mi")
                                        }
                                    },
                                    VolumeMounts = new List<V1VolumeMount>
                                    {
                                        new V1VolumeMount("/mnt/perf", "volume")
                                    },
                                    Command = testCategory == TestCategory.AspnetSignalR
                                        ? new List<string>
                                        {
                                            "powershell"
                                        }
                                        : new List<string>
                                        {
                                            "/bin/sh", "-c"
                                        },
                                    Args = GetStartArg(testCategory, server),
                                    Env = new List<V1EnvVar>
                                    {
                                        new V1EnvVar(PerfConstants.ConfigurationKeys.PodNameStringKey,
                                            valueFrom: new V1EnvVarSource(
                                                fieldRef: new V1ObjectFieldSelector("metadata.name"))),
                                        new V1EnvVar(PerfConstants.ConfigurationKeys.TestIdKey, testId),
                                        new V1EnvVar(PerfConstants.ConfigurationKeys.ConnectionString,
                                            string.Join(",", asrsConnectionStrings)),
                                        new V1EnvVar(PerfConstants.ConfigurationKeys.StorageConnectionStringKey,
                                            _perfStorageProvider.ConnectionString),
                                        new V1EnvVar(PerfConstants.ConfigurationKeys.RedisConnectionStringKey,
                                            _redisConnectionString),
                                        new V1EnvVar(PerfConstants.ConfigurationKeys.Protocol,
                                            formatProtocol),
                                    }
                                }
                            },
                            Volumes = new List<V1Volume>
                            {
                                new V1Volume("volume")
                                {
                                    AzureFile = new V1AzureFileVolumeSource("azure-secret", "perf", false)
                                }
                            }
                        }
                    }
                }
            };
            if (_internal && (testCategory == TestCategory.AspnetCoreSignalRServerless ||
                    testCategory == TestCategory.RawWebsocket && behavior == ClientBehavior.Echo))
            {
                deployment.Spec.Template.Spec.Containers.Add(new V1Container
                {
                    Name = "proxy",
                    Image =
                        _image,
                    Resources = new V1ResourceRequirements
                    {
                        Requests = new Dictionary<string, ResourceQuantity>
                        {
                            ["cpu"] = new ResourceQuantity("500m"),
                            ["memory"] = new ResourceQuantity("1000Mi")
                        },
                        Limits = new Dictionary<string, ResourceQuantity>
                        {
                            ["cpu"] = new ResourceQuantity("500m"),
                            ["memory"] = new ResourceQuantity("1000Mi")
                        }
                    },
                    Command =
                        new List<string>
                        {
                            "/bin/sh", "-c"
                        },
                    Args =
                        new List<string>
                        {
                            "cd /home; ./start_proxy_client.sh"
                        },
                    Env = new List<V1EnvVar>
                    {
                        new V1EnvVar("connectCount", "200"
                        ),
                        new V1EnvVar("proxyPort",
                            testCategory == TestCategory.AspnetCoreSignalRServerless ? "8100" : "8101"
                        ),
                    }
                });
            }
            await _k8S.CreateNamespacedDeploymentAsync(deployment, Default, cancellationToken: cancellationToken);

            if (serverPodCount == 0 || testCategory == TestCategory.SocketIO)
            {
                return asrsConnectionStrings[0];
            }
            else if (testCategory == TestCategory.RawWebsocket || testCategory == TestCategory.WebPubSubPythonSdk || testCategory == TestCategory.Mqtt)
            {
                return asrsConnectionStrings[0] + "," + name;
            }
            else
            {
                return name;
            }
        }

        public async Task CreateClientPodsAsync(TestJob testJob,TestStatusEntity statusEntity, int clientPodCount,
            CancellationToken cancellationToken)
        {
            var testId = testJob.TestId;
            var testCategory = testJob.TestMethod;
            var name = NameConverter.Truncate(testId);
            V1StatefulSet statefulSet = new V1StatefulSet()
            {
                Metadata = new V1ObjectMeta
                {
                    Name = name,
                    Labels = new Dictionary<string, string>
                    {
                        // [PerfConstants.ConfigurationKeys.TestIdKey] = testId
                        ["type"] = Client
                    },
                    Annotations = new Dictionary<string, string>
                    {
                        ["cluster-autoscaler.kubernetes.io/safe-to-evict"] = "false",
                        [PerfConstants.ConfigurationKeys.TestIdKey] = testId
                    }
                },
                Spec = new V1StatefulSetSpec()
                {
                    Replicas = clientPodCount,
                    ServiceName = name,
                    Selector = new V1LabelSelector
                    {
                        MatchLabels = new Dictionary<string, string>
                        {
                            { "app", name }
                        }
                    },
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta
                        {
                            CreationTimestamp = null,
                            Labels = new Dictionary<string, string>
                            {
                                ["app"] = name,
                                ["type"]= Client
                            }
                        },
                        Spec = new V1PodSpec
                        {
                            DnsConfig = new V1PodDNSConfig
                            {
                                Options = new List<V1PodDNSConfigOption>
                                {
                                    new V1PodDNSConfigOption
                                    {
                                        Name = "ndots",
                                        Value = "2"
                                    }
                                }
                            },
                            NodeSelector = new Dictionary<string, string>
                            {
                                // [PerfConstants.Name.OsLabel] = PerfConstants.Name.Linux
                            },
                            Containers = new List<V1Container>
                            {
                                new V1Container
                                {
                                    Name = name,
                                    Image = _image,
                                    Resources = new V1ResourceRequirements
                                    {
                                        Requests = new Dictionary<string, ResourceQuantity>
                                        {
                                            ["cpu"] = new ResourceQuantity("3000m"),
                                            ["memory"] = new ResourceQuantity("10000Mi")
                                        },
                                        Limits = new Dictionary<string, ResourceQuantity>
                                        {
                                            ["cpu"] = new ResourceQuantity("4000m"),
                                            ["memory"] = new ResourceQuantity("14000Mi")
                                        }
                                    },
                                    VolumeMounts = new List<V1VolumeMount>
                                    {
                                        new V1VolumeMount("/mnt/perf", "volume")
                                    },
                                    Command = new List<string>
                                    {
                                        "/bin/sh", "-c"
                                    },
                                    Args = new List<string>
                                    {
                                        "cp /mnt/perf/manifest/Client/Client.zip /home ; cd /home ; unzip Client.zip ; exec ./Client"
                                    },
                                    Ports = new List<V1ContainerPort>()
                                    {
                                        new V1ContainerPort()
                                        {
                                            ContainerPort = 8080,
                                            Name = "metrics"
                                        }
                                    },
                                    Env = new List<V1EnvVar>
                                    {
                                        new V1EnvVar(PerfConstants.ConfigurationKeys.PodNameStringKey,
                                            valueFrom: new V1EnvVarSource(
                                                fieldRef: new V1ObjectFieldSelector("metadata.name"))),
                                        new V1EnvVar(PerfConstants.ConfigurationKeys.TestIdKey, testId),
                                        new V1EnvVar(PerfConstants.ConfigurationKeys.StorageConnectionStringKey,
                                            _perfStorageProvider.ConnectionString),
                                        new V1EnvVar(PerfConstants.ConfigurationKeys.CosmosConnectionStringKey,
                                            _perfStorageProvider.CosmosConnectionString),
                                        new V1EnvVar(PerfConstants.ConfigurationKeys.RedisConnectionStringKey,
                                            _redisConnectionString),
                                        new V1EnvVar(PerfConstants.ConfigurationKeys.TestCategory,
                                            testCategory.ToString()),
                                        new V1EnvVar(PerfConstants.ConfigurationKeys.TestStatusPartitionKey,statusEntity.PartitionKey),
                                        new V1EnvVar(PerfConstants.ConfigurationKeys.TestStatusRowKey,statusEntity.RowKey),
                                    }
                                }
                            },
                            Volumes = new List<V1Volume>
                            {
                                new V1Volume("volume")
                                {
                                    AzureFile = new V1AzureFileVolumeSource("azure-secret", "perf", false)
                                }
                            }
                        }
                    }
                }
            };
            await _k8S.CreateNamespacedStatefulSetAsync(statefulSet, Default, cancellationToken: cancellationToken);
        }

        public async Task DeleteClientPodsAsync(string testId)
        {
            var name = NameConverter.Truncate(testId);
            // await _k8S.DeleteNamespacedDeploymentAsync(name, Default);
            try
            {
                await _k8S.DeleteNamespacedStatefulSetAsync(name, Default);
            }catch (Microsoft.Rest.HttpOperationException e) when (e.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // ignore
            }
        }

        public async Task DeleteServerPodsAsync(string testId, bool upstream)
        {
            var name = NameConverter.Truncate(testId);
            
            try
            {
                await _k8S.DeleteNamespacedServiceAsync(name, Default);
            }catch (Microsoft.Rest.HttpOperationException e) when (e.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // ignore
            }
            
            if (upstream)
            {
                try
                {
                    await _k8S.DeleteNamespacedIngress1Async(NameConverter.Truncate(testId), Default);
                }catch (Microsoft.Rest.HttpOperationException e) when (e.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // ignore
                }
            }
            
            try
            {
                await _k8S.DeleteNamespacedDeploymentAsync(name, Default);
            }catch (Microsoft.Rest.HttpOperationException e) when (e.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // ignore
            }
        }

        private static IList<string> GetStartArg(TestCategory testCategory, string server)
        {
            switch (testCategory)
            {
                case TestCategory.AspnetSignalR:
                    return
                        new List<string>
                        {
                            "cd  /mnt/perf/manifest; xcopy .\\AspNetAppServer\\AspNetAppServer.zip C:\\home\\ ; cd C:/home/ ; tar -xf AspNetAppServer.zip ; ./AspNetAppServer.exe"
                        };
                case TestCategory.SocketIO:
                    return new List<string>
                    {
                        $"cp /mnt/perf/manifest/{server}/{server}.zip /home ; cd /home ; unzip {server}.zip ; NODE_ENV=production node server.js;"
                    };
                case TestCategory.WebPubSubPythonSdk:
                    return new List<string>
                    {
                        $"cp /mnt/perf/manifest/{server}/{server}.zip /home ; cd /home ; unzip {server}.zip ; pip install pyserver-1.0.0-py3-none-any.whl --break-system-packages; start-server;"
                    };
                default:
                    return
                        new List<string>
                        {
                            $"cp /mnt/perf/manifest/{server}/{server}.zip /home ; cd /home ; unzip {server}.zip ;exec ./{server};"
                        };
            }
        }
    }
}