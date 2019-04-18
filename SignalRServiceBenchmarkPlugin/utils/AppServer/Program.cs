// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System;

namespace Microsoft.Azure.SignalR.PerfTest.AppServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args).UseKestrel(KestrelConfig)
                .UseStartup<Startup>();

        public static readonly Action<WebHostBuilderContext, KestrelServerOptions> KestrelConfig =
            (context, options) =>
            {
                options.
            };
    }
}
