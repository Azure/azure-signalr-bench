using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace MDMSystemLoadQueryService
{
    public class DefaultMDMQuery : IMDMQuery
    {
        private MDMOptions _mDMOptions;

        public DefaultMDMQuery(IOptions<MDMOptions> mDMOptions)
        {
            _mDMOptions = mDMOptions.Value;
        }

        public string QueryMetrics(PlatformType platformType, SystemLoadType systemLoadType, string podName, string startTime, string endTime)
        {
            if (platformType == PlatformType.Dogfood)
            {
                var result = InvokeExternalExe(platformType, systemLoadType, podName, startTime, endTime);
                return result;
            }
            return null;
        }

        private string InvokeExternalExe(PlatformType platformType, SystemLoadType systemLoadType, string podName, string startTime, string endTime)
        {
            var exePath = _mDMOptions.ExternalExePath;//"C:/home/Work/DevDiv/MDMetricsClientSampleCode/bin/Debug/MDMetricsClientSampleCode.exe";
            var argsBuilder = new StringBuilder();
            argsBuilder.Append(podName).Append(" ")
                .Append(platformType.ToString()).Append(" ")
                .Append(systemLoadType.ToString()).Append(" ")
                .Append(startTime).Append(" ")
                .Append(endTime).Append(" ")
                .Append(_mDMOptions.ResultPath);
            var task = RunProcessAsync(exePath, argsBuilder.ToString());
            if (task && File.Exists(_mDMOptions.ResultPath))
            {
                string content;
                using (StreamReader file = new StreamReader(_mDMOptions.ResultPath, true))
                {
                    content = file.ReadToEnd();
                }
                File.Delete(_mDMOptions.ResultPath);
                return content;
            }
            else
            {
                Console.WriteLine("Exe has not completed successfully!");
            }
            return null;
        }

        public static bool RunProcessAsync(string processPath, string arguments)
        {
            Console.WriteLine($"{processPath} {arguments}");
            var process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo(processPath)
                {
                    Arguments = arguments,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };
            if (!process.Start())
            {
                Console.WriteLine("Fail to launch exe!");
                return false;
            }
            process.WaitForExit();
            return true;
        }
    }
}
