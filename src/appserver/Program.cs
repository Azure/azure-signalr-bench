// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.SignalR.PerfTest.AppServer
{
    public class Program
    {
        private const string Port = "Port";
        private const string HttpsEnabled = "Https:Enabled";
        private const string HttpsLocalCertPath = "Https:LocalCertPath";
        private const string UseLocalSignalR = "useLocalSignalR";
        private const string UserSecrets = "appserver";
        private const string ASRSConnectionStringKey = "Azure:SignalR:ConnectionString";
        private const string ASRSConnectionNumberKey = "Azure:SignalR:ConnectionNumber";
        private const int DefaultPort = 5050;

        public static void Main(string[] args)
        {
            var appConfig = GenServerConfig();
            WebHost.CreateDefaultBuilder(args)
                .UseKestrel(KestrelConfig)
                .UseStartup<Startup>()
                .ConfigureServices(s => s.AddSingleton(appConfig))
                .Build().Run();
        }    

        private static AppServerConfig GenServerConfig()
        {
            var signalrType =
                Environment.GetEnvironmentVariable(UseLocalSignalR) == null ||
                Environment.GetEnvironmentVariable(UseLocalSignalR) == "" ||
                Environment.GetEnvironmentVariable(UseLocalSignalR) == "false" ? 1 : 0;
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddUserSecrets(UserSecrets)
                .Build();
            var appConfig = new AppServerConfig()
            {
                SignalRType = signalrType
            };
            if (signalrType == 1)
            {
                var connectionString = config[ASRSConnectionStringKey];
                Console.WriteLine($"connection string: {connectionString}");
                appConfig.ConnectionString = connectionString;
                if (config[ASRSConnectionNumberKey] != null)
                {
                    appConfig.ConnectionNumber = config.GetValue<int>(ASRSConnectionNumberKey);
                }
            }
            return appConfig;
        }

        private static readonly Action<WebHostBuilderContext, KestrelServerOptions> KestrelConfig =
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
