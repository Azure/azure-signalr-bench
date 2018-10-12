using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Base
{
    public interface ISlaveMethod
    {
        Task Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters);

    }
}
