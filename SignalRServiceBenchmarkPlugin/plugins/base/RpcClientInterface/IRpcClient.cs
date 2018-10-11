using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Rpc.Service
{
    // Decouple concrete Rpc client from plugin
    public interface IRpcClient
    {
        Task UpdateAsync(IDictionary<string, object> data);
        Task<IDictionary<string, object>> QueryAsync(IDictionary<string, object> data);
        IRpcClient Create(string hostname, int port);
        bool TestConnection();
        Task<bool> InstallPluginAsync(string pluginName);
        string Serialize(IDictionary<string, object> data);
        bool CheckTypeAndMethod(IDictionary<string, object> data);

    }
}
