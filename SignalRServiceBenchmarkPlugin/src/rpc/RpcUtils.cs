using Serilog;
using System.IO;

namespace Rpc.Service
{
    public class RpcUtils
    {
        public static void CreateLogger(string directory, string name, RpcLogTargetEnum logTarget)
        {
            // remove history logs
            foreach (string f in Directory.EnumerateFiles(directory, name.Replace(".", "*")))
            {
                File.Delete(f);
            }
            switch (logTarget)
            {

                case RpcLogTargetEnum.File:
                    Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File(Path.Combine(directory, name), rollingInterval: RollingInterval.Day)
                    .CreateLogger();
                    break;
                case RpcLogTargetEnum.Console:
                    Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.Console()
                    .CreateLogger();
                    break;
                default:
                case RpcLogTargetEnum.All:
                    Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.Console()
                    .WriteTo.File(Path.Combine(directory, name), rollingInterval: RollingInterval.Day)
                    .CreateLogger();
                    break;
            }
        }
    }
}
