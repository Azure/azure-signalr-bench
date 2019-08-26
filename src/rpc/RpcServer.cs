using Grpc.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rpc.Service
{
    public class RpcServer : IRpcServer
    {
        private Server _server;
        private string _hostname;
        private int _port;

        public IRpcServer Create(string hostname, int port)
        {
            _hostname = hostname;
            _port = port;
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
            _server.Start();
            Console.WriteLine($"Listening on: {_hostname}:{_port}");
            Console.WriteLine("Press CTRL+C to exit");
            return Task.Delay(Timeout.Infinite);
        }

        public Task Stop()
        {
            return _server.ShutdownAsync();
        }
    }
}
