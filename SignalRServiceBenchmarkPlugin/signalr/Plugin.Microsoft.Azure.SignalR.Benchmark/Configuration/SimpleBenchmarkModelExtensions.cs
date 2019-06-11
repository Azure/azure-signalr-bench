using Serilog;
using System.Collections.Generic;
using static Plugin.Microsoft.Azure.SignalR.Benchmark.SimpleBenchmarkModel;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    public static class SimpleBenchmarkModelExtensions
    {
        public enum ERRORCODE
        {
            NoErr,
            InvalidKind = 1,
            InvalidConnectionType,
            MissingTarget
        }

        public static IDictionary<ERRORCODE, string> ErrorMap = new Dictionary<ERRORCODE, string>();

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

        public static bool IsDebug(this BenchConfigData configData)
        {
            return configData.Config.Debug;
        }

        public static bool isPerf(this BenchConfigData configData)
        {
            return configData.Kind == DEFAULT_KIND;
        }

        public static bool isLongrun(this BenchConfigData configData)
        {
            return configData.Kind == LONGRUN_KIND;
        }

        public static bool isResultParser(this BenchConfigData configData)
        {
            return configData.Kind == PARSERESULT_KIND;
        }

        public static ERRORCODE isValid(this BenchConfigData configData)
        {
            string error = null;
            if (!configData.isLongrun() && !configData.isPerf() && !configData.isResultParser())
            {
                error = $"Kind must be {DEFAULT_KIND} or {LONGRUN_KIND} or {PARSERESULT_KIND}, but see {configData.Kind}";
                ErrorMap[ERRORCODE.InvalidKind] = error;
                Log.Error(error);
                return ERRORCODE.InvalidKind;
            }
            if (!configData.IsCore() && !configData.IsAspNet())
            {
                error = $"ConnectionType must be {DEFAULT_CONNECTION_TYPE} or {ASPNET_CONNECTION_TYPE}, but see {configData.Config.ConnectionType}";
                ErrorMap[ERRORCODE.InvalidConnectionType] = error;
                Log.Error(error);
                return ERRORCODE.InvalidConnectionType;
            }
            if (string.IsNullOrEmpty(configData.Config.ConnectionString) &&
                string.IsNullOrEmpty(configData.Config.WebAppTarget))
            {
                error = $"ConnectionString and WebAppTarget cannot be empty at the same time. You must specify any of them.";
                ErrorMap[ERRORCODE.MissingTarget] = error;
                Log.Error(error);
                return ERRORCODE.MissingTarget;
            }
            var connections = configData.Config.Connections;
            var baseSending = configData.Config.BaseSending;
            if (baseSending > connections)
            {
                Log.Warning($"Base sending numbers should not be larger than total connections, will be changed to the same as connections.");
                configData.Config.BaseSending = connections;
            }
            return ERRORCODE.NoErr;
        }
    }
}
