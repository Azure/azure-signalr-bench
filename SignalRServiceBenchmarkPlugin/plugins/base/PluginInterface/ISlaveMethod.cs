﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plugin.Base
{
    public interface ISlaveMethod
    {
        Task<IDictionary<string, object>> Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters);

    }
}
