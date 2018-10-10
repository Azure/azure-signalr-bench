using Grpc.Core;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Rpc.Service
{
    public class RpcClient : IRpcClient
    {
        private RpcService.RpcServiceClient _client;

        public Task<string> QueryAsync(string data)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(string data)
        {
            return _client.UpdateAsync(new Data { Json = data }).ResponseAsync;
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
            var result = await _client.InstallPluginAsync(new Data { Json = pluginName }).ResponseAsync;
            if (!result.Success) Log.Error($"Fail to install plugin in slave: {result.Message}");
            return result.Success;
        }
    }
}
