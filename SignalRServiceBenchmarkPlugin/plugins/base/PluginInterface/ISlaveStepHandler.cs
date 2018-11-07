using Rpc.Service;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Base
{
    public interface ISlaveStepHandler
    {
        IDictionary<string, object> PluginSlaveParamaters { get; set; }
        ISlaveMethod CreateSlaveMethodInstance(string methodName);
    }
}
