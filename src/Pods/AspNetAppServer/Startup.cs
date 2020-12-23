// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Azure.SignalRBench.Common;
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
            var p = GetType().FullName;
            app.MapAzureSignalR(p, options =>
            {
                options.ConnectionString = Environment.GetEnvironmentVariable(PerfConstants.ConfigurationKeys.ConnectionString);
                options.ConnectionCount = 5;
            });
        }
    }
}
