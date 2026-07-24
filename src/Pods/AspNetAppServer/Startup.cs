// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Azure.SignalRBench.Common;
using Microsoft.Azure.SignalR;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Owin;

[assembly: OwinStartup(typeof(AspNetAppServer.Startup))]

namespace AspNetAppServer
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseCors(CorsOptions.AllowAll);
            app.MapAzureSignalR(GetType().FullName, options =>
            {
                var credential = SignalRManagedIdentity.CreateCredential(
                    Environment.GetEnvironmentVariable(PerfConstants.ConfigurationKeys.MsiAppId));
                options.Endpoints = SignalRManagedIdentity.ParseEndpoints(
                        Environment.GetEnvironmentVariable(PerfConstants.ConfigurationKeys.ConnectionString))
                    .Select(endpoint => new ServiceEndpoint(endpoint, credential))
                    .ToArray();
                options.ConnectionCount = 5;
            });
        }
    }
}