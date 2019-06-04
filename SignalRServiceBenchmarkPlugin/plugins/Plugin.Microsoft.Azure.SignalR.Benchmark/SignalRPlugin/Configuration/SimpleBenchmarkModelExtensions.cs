using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using static Plugin.Microsoft.Azure.SignalR.Benchmark.SimpleBenchmarkModel;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    public static class SimpleBenchmarkModelExtensions
    {
        public static bool IsCore(this BenchConfigData configData)
        {
            return configData.Config.ConnectionType == DEFAULT_CONNECTION_TYPE;
        }

        public static bool IsAspNet(this BenchConfigData configData)
        {
            return configData.Config.ConnectionType == ASPNET_CONNECTION_TYPE;
        }

        public static bool IsDirect(this BenchConfigData configData)
        {
            return configData.Scenario.Name.StartsWith(DIRECT_CONNECTION_PREFIX);
        }

        public static bool isPerf(this BenchConfigData configData)
        {
            return configData.Kind == DEFAULT_KIND;
        }

        public static bool isLongrun(this BenchConfigData configData)
        {
            return configData.Kind == LONGRUN_KIND;
        }

        public static bool isValid(this BenchConfigData configData)
        {
            if (!configData.isLongrun() && !configData.isPerf())
            {
                Log.Error($"Kind must be {DEFAULT_KIND} or {LONGRUN_KIND}, but see {configData.Kind}");
                return false;
            }
            if (!configData.IsCore() && !configData.IsAspNet())
            {
                Log.Error($"ConnectionType must be {DEFAULT_CONNECTION_TYPE} or {ASPNET_CONNECTION_TYPE}, but see {configData.Config.ConnectionType}");
                return false;
            }
            var connections = configData.Config.Connections;
            var baseSending = configData.Config.BaseSending;
            if (baseSending > connections)
            {
                Log.Warning($"base sending should not be larger than total connections, will be changed to the same as connections");
                configData.Config.BaseSending = connections;
            }
            return true;
        }
    }
}
