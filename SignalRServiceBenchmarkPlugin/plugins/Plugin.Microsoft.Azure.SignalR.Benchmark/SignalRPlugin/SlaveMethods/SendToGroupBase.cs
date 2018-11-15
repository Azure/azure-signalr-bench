using Plugin.Base;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    class SendToGroupBase : ISlaveMethod
    {
        public Task<IDictionary<string, object>> Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters)
        {
            
        }

        protected virtual void LoadParameters()
        {

        }
    }
}
