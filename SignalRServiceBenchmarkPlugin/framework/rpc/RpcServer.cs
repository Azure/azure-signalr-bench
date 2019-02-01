using Grpc.Core;
using Serilog;
using System.Threading;
using System.Threading.Tasks;

namespace Rpc.Service
{
    public class RpcServer : IRpcServer
    {
        private Server _server;
        public IRpcServer Create(string hostname, int port)
        {
            Log.Information("Create Rpc Server...");

            _server = new Grpc.Core.Server(new ChannelOption[]
            {
                // For Group, the received message size is very large, so here set 8000k
                new ChannelOption(ChannelOptions.MaxReceiveMessageLength, 8192000)
            })
            {
                Services = { RpcService.BindService(new RpcServiceImpl()) },
                Ports = { new ServerPort(hostname, port, ServerCredentials.Insecure) }
            };
            return this;
        }

        public Task Start()
        {
            Log.Information("Start server...");
            _server.Start();
            return Task.Delay(Timeout.Infinite);
        }
    }
}
