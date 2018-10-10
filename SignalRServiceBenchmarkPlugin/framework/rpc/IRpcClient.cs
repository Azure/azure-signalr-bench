using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Rpc.Service
{
    // Decouple concrete Rpc client from plugin
    public interface IRpcClient
    {
        Task UpdateAsync(string data);
        Task<string> QueryAsync(string data);
        IRpcClient Create(string hostname, int port);
        bool TestConnection();
        Task<bool> InstallPluginAsync(string pluginName);
    }
}
