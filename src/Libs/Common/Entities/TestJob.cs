// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Azure.SignalRBench.Common
{
    public class TestJob
    {
        public string TestId { get; set; } = string.Empty;
        public TestCategory TestMethod { get; set; }
        public ServiceSetting ServiceSetting { get; set; } = new ServiceSetting();
        public ScenarioSetting ScenarioSetting { get; set; } = new ScenarioSetting();
    }
}
