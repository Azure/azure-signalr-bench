// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.SignalR.PerfTest.AppServer
{
    public class Program
    {
        private const string Port = "Port";
        private const string HttpsEnabled = "Https:Enabled";
        private const string HttpsLocalCertPath = "Https:LocalCertPath";
        private const int DefaultPort = 5050;

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
                var config = context.Configuration;
                // After we apply Nginx, all of requests will map to the same port.
                if (!int.TryParse(config[Port], out var port))
                {
                    port = DefaultPort;
                }
                Console.WriteLine(config[HttpsEnabled]);
                if (bool.TryParse(config[HttpsEnabled], out bool isHttps) && isHttps)
                {
                    var localCertPath = config[HttpsLocalCertPath];
                    X509Certificate2 cert;
                    if (!string.IsNullOrEmpty(localCertPath))
                    {
                        // Use a local cert
                        cert = new X509Certificate2(localCertPath);
                        options.ListenAnyIP(port, listenOptions => listenOptions.UseHttps(cert));
                        Console.WriteLine("apply https");
                    }
                    else
                    {
                        options.ListenAnyIP(port);
                    }
                }
                else
                {
                    Console.WriteLine("apply http");
                    options.ListenAnyIP(port);
                }
            };
    }
}
