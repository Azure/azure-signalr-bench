using System;
using JenkinsScript;
using Xunit;

namespace JenkinsScripts.Tests
{
    public class StatisticsCollectorTests
    {
        [Fact]
        public void GenerateDirectories()
        {
            var x = new StatisticsCollector("/parent", "root", "scenario");
            Assert.True(x.MachineDirPath == "/parent/root/scenario/machine", $"{x.MachineDirPath} is not a valid path");
            Assert.True(x.ConfigDirPath == "/parent/root/scenario/config", $"{x.ConfigDirPath} is not a valid path");
            Assert.True(x.LogDirPath == "/parent/root/scenario/log", $"{x.LogDirPath} is not a valid path");
        }
    }
}