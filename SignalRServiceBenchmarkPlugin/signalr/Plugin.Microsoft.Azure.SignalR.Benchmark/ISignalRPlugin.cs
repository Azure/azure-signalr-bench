using Plugin.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    interface ISignalRPlugin
    {
        IDictionary<string, object> PluginMasterParameters { get; set; }

        IMasterMethod CreateMasterMethodInstance(string methodName);

        IDictionary<string, object> PluginSlaveParamaters { get; set; }

        ISlaveMethod CreateSlaveMethodInstance(string methodName);
    }
}
