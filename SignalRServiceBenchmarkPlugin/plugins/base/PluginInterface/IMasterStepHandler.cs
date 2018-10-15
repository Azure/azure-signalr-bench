using Rpc.Service;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Base
{
    public interface IMasterStepHandler
    {
        IDictionary<string, object> PluginMasterParameters { get; set; }
        IMasterMethod CreateMasterMethodInstance(string methodName);
    }
}
