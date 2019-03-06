// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Owin;
using System;
using System.Diagnostics;

[assembly: OwinStartup(typeof(AspNetSelfhostServer.Startup))]
namespace AspNetSelfhostServer
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var config = new Configuration();
            // Any connection or hub wire up and configuration should go here
            if (config.UseLocalSignalR)
            {
                Console.WriteLine("Using local SignalR");
                app.RunSignalR();
            }
            else
            {
                app.RunAzureSignalR(GetType().FullName, config.ConnectionString);
            }
            GlobalHost.TraceManager.Switch.Level = SourceLevels.Information;
        }
    }
}
