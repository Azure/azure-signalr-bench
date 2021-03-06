﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Rpc.Service
{
    // Decouple concrete Rpc client from plugin
    public interface IRpcClient
    {
        Task<IDictionary<string, object>> QueryAsync(IDictionary<string, object> data);

        bool TestConnection();

        Task<bool> InstallPluginAsync(string pluginName);

        void InstallSerializerAndDeserializer(Func<IDictionary<string, object>, string> serialize, Func<string, IDictionary<string, object>> deserialize);

        Func<IDictionary<string, object>, string> Serialize { get; set; }

        Func<string, IDictionary<string, object>> Deserialize { get; set; }
    }
}
