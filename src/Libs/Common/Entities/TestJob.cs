﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Azure.SignalRBench.Common
{
    public class TestJob
    {
        public string TestId { get; set; } = string.Empty;
        public TestCategory TestMethod { get; set; }
        public ServiceSetting[] ServiceSetting { get; set; } = Array.Empty<ServiceSetting>();
        public ScenarioSetting ScenarioSetting { get; set; } = new ScenarioSetting();
        public ServerSetting ServerSetting { get; set; } = new ServerSetting();
    }
}
