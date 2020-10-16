// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Newtonsoft.Json.Linq;

namespace Azure.SignalRBench.Coordinator
{
    public interface IArmProvider
    {
        Task Deploy(string deploymentName, JObject deployTemplate, JObject deployParams);
        void Initialize(AzureCredentials credentials, string subscription, string resourceGroup);
    }
}