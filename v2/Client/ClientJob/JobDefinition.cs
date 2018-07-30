using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Client.ClientJobNs
{
    class JobDefinition : Dictionary<string, JObject>
    {
        public JobDefinition() : base(StringComparer.OrdinalIgnoreCase)
        {
        }
    }
}
