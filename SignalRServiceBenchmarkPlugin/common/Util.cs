using Serilog;
using System;
using System.IO;

namespace Common
{
    public class Util
    {
        public static void CreateLogger(string directory, string name)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(Path.Combine(directory, name), rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }
    }
}
