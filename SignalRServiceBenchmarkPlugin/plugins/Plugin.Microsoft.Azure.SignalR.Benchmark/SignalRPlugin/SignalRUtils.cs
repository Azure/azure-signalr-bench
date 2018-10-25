using System;
using System.Collections.Generic;
using System.Text;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    public static class SignalRUtils
    {
        public static string GroupName(string type, int index) => $"{type}:{index}";
        public static string MessageLessThan(long latency) => $"message:lt:{latency}";
        public static string MessageGreaterOrEqaulTo(long latency) => $"message:ge:{latency}";
    }
}
