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
        bool CheckTypeAndMethod(IDictionary<string, object> data);
        void InstallSerializerAndDeserializer(Func<IDictionary<string, object>, string> serialize, Func<string, IDictionary<string, object>> deserialize);
        Func<IDictionary<string, object>, string> Serialize { get; set; }
        Func<string, IDictionary<string, object>> Deserialize { get; set; }
    }
}
