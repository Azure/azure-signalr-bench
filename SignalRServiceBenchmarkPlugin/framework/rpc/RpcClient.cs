using Common;
using Grpc.Core;
using Newtonsoft.Json;
using Plugin.Base;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rpc.Service
{
    public class RpcClient : IRpcClient
    {
        private RpcService.RpcServiceClient _client;

        public async Task<IDictionary<string, object>> QueryAsync(IDictionary<string, object> data)
        {
            if (!CheckTypeAndMethod(data))
            {
                var message = $"Do not contain {Constants.Type} and {Constants.Method}.";
                Log.Error(message);
                throw new Exception(message);
            }
            try
            {
                var result = await _client.QueryAsync(new Data { Json = RpcUtil.Serialize(data) }).ResponseAsync;
                if (!result.Success) throw new Exception(result.Message);
                var returnData = RpcUtil.Deserialize(result.Json);
                return returnData;
            }
            catch (Exception ex)
            {
                var message = $"Rpc error: {ex}";
                Log.Error(message);
                throw new Exception(message);
            }
        }

        public Task UpdateAsync(IDictionary<string, object> data)
        {
            if (!CheckTypeAndMethod(data))
            {
                var message = $"Do not contain {Constants.Type} and {Constants.Method}.";
                Log.Error(message);
                throw new Exception(message);
            }
            return _client.UpdateAsync(new Data { Json = RpcUtil.Serialize(data) }).ResponseAsync;
        }

        private static Channel CreateRpcChannel(string hostname, int port)
        {
            Log.Information("Open channel to rpc server...");
                var channel = new Channel($"{hostname}:{port}", ChannelCredentials.Insecure,
                    new ChannelOption[] {
                        // For Group, the received message size is very large, so here set 8000k
                        new ChannelOption(ChannelOptions.MaxReceiveMessageLength, 8192000)
                    });

            return channel;
        }

        private static RpcService.RpcServiceClient CreateRpcClient(Channel channel)
        {
            Log.Information($"Create Rpc client...");
            var client = new RpcService.RpcServiceClient(channel);
            return client;
        }

        public IRpcClient Create(string hostname, int port)
        {
            var channel = CreateRpcChannel(hostname, port);
            _client = CreateRpcClient(channel);
            return this;
        }

        public bool TestConnection()
        {
            var result = _client.TestConnection(new Empty());
            return result.Success;
        }

        public async Task<bool> InstallPluginAsync(string pluginName)
        {
            Log.Information($"Install plugin '{pluginName}' in slave...");
            var result = await _client.InstallPluginAsync(new Data { Json = pluginName }).ResponseAsync;
            if (!result.Success) Log.Error($"Fail to install plugin in slave: {result.Message}");
            return result.Success;
        }

        public bool CheckTypeAndMethod(IDictionary<string, object> data)
        {
            if (data.ContainsKey(Constants.Type) && data.ContainsKey(Constants.Method)) return true;
            return false;
        }
    }
}
