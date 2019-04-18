using System.Collections.Generic;

namespace Plugin.Base
{
    // User can implement Iplugin to handle step in master and in slaves
    public interface IPlugin: IMasterStepHandler, ISlaveStepHandler
    {
        string Serialize(IDictionary<string, object> data);
        Dictionary<string, object> Deserialize(string input);
    }
}
