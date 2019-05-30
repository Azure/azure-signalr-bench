using Rpc.Service;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plugin.Base
{
    // User can implement Iplugin to handle step in master and in slaves
    public interface IPlugin
    {
        // serialize <key, value> to JSON string
        string Serialize(IDictionary<string, object> data);

        // deserialize JSON string to <key, value>
        Dictionary<string, object> Deserialize(string input);

        // Start the Plugin's benchmark
        Task Start(string configurationContent, IList<IRpcClient> clients);

        // Execute command on agent node, the input/output are Json strings
        Task<string> ExecuteOnAgent(string parametersInJson);

        void Help();

        void DumpConfiguration(string configuration);
    }
}
