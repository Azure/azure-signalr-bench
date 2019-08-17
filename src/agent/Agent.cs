using Common;
using Rpc.Service;
using System.Threading.Tasks;

namespace Rpc.Agent
{
    public class Agent
    {
        private RpcConfig _rpcConfig;

        public Agent(RpcConfig config)
        {
            _rpcConfig = config;
        }

        public async Task Start()
        {
            Util.SavePidToFile(_rpcConfig.PidFile);

            // Create Logger
            RpcUtils.CreateLogger(_rpcConfig.LogDirectory, _rpcConfig.LogName, _rpcConfig.LogTarget);

            // Create Rpc server
            var server = new RpcServer().Create(_rpcConfig.HostName, _rpcConfig.RpcPort);

            // Start Rpc server
            await server.Start();
        }
    }
}
