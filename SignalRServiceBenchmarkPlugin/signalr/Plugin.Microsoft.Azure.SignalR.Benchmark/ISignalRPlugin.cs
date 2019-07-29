using Plugin.Base;
using System.Collections.Generic;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    interface ISignalRPlugin
    {
        IDictionary<string, object> PluginMasterParameters { get; set; }

        IMasterMethod CreateMasterMethodInstance(string methodName);

        IDictionary<string, object> PluginAgentParamaters { get; set; }

        IAgentMethod CreateAgentMethodInstance(string methodName);
    }
}
