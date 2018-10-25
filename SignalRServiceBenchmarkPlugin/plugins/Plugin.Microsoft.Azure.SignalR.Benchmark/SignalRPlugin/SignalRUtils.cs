using System;
using System.Collections.Generic;
using System.Text;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    public static class SignalRUtils
    {
        public static string GroupName(string type, int index) => $"{type}:{index}";
    }
}
