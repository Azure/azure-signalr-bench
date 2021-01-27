using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.SignalR.PerfTest.AppServer
{
    public class AppServerConfig
    {
        // 0 means self-host SignalR, 1 means ASRS. Default is 0.
        public int SignalRType { get; set; } = 0;

        // in hour
        public int AccessTokenLifetime { get; set; } = 24;

        public int ConnectionNumber { get; set; } = 5;

        public string ConnectionString { get; set; }

        public LogLevel MinLogLevel { get; set; } = LogLevel.Debug;
    }
}
