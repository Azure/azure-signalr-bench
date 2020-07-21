// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.SignalRBench.Tests
{
    public class TestConfiguration
    {
        public static readonly TestConfiguration Instance = new TestConfiguration();

        private readonly IConfiguration _configuration;

        private TestConfiguration()
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            Init();
        }

        private void Init()
        {
            Redis = _configuration["redis"];
        }

        public string Redis { get; set; }
    }
}
