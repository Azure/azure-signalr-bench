using Rpc.Service;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plugin.Base
{
    // User can implement Iplugin to handle step in master and in slaves
    public interface IPlugin
    {
        // Serialize <key, value> to JSON string
        string Serialize(IDictionary<string, object> data);

        // Deserialize JSON string to <key, value>
        Dictionary<string, object> Deserialize(string input);

        // Start the Plugin's benchmark
        Task Start(string configurationContent, IList<IRpcClient> clients);

        // Execute command on agent node, the input/output are Json strings
        Task<string> ExecuteOnAgent(string parametersInJson);

        // Check whether the slave nodes are necessary
        bool NeedSlaves(string configuration);

        // Print the help information for commandline or configurations
        void Help();
    }
}
