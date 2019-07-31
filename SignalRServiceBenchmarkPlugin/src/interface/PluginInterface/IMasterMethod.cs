using Rpc.Service;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plugin.Base
{
    public interface IMasterMethod
    {
        Task Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters, IList<IRpcClient> clients);
    }
}