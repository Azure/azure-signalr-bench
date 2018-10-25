using System;
using System.IO;
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
            Assert.True(x.ResultDirPath == "/parent/root/scenario/result", $"{x.ResultDirPath} is not a valid path");
        }

        [Fact]
        public void Collect()
        {
            var x = new StatisticsCollector("./test", "root", "scenario");
            var src = "./config.txt";
            var dst = Path.Combine(x.ConfigDirPath, Path.GetFileName(src));
            var content = "Copy config file";
            
            File.AppendAllText(src, content);
            x.CollectConfig(src);
            
            var copiedContent = File.ReadAllText(Path.Combine(x.ConfigDirPath, Path.GetFileName(src)));
            Assert.True(content == copiedContent, "Wrong file content: {copiedContent} after copy");
            
            var exist = File.Exists(dst);
            File.Delete(src);
            if (Directory.Exists("./test")) Directory.Delete("./test", true);
            Assert.True(exist, $"Fail to copy config file from {src} to {dst}");
        }
    }
}