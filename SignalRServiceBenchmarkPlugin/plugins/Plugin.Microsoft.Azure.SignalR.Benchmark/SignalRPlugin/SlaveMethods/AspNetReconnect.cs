using Plugin.Base;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public class AspNetReconnect : ReconnectBase, ISlaveMethod
    {
        public async Task<IDictionary<string, object>> Do(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters)
        {
            var ret = await RunReconnect(stepParameters, pluginParameters);
            return ret;
        }
    }
}
