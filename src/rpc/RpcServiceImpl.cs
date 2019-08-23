using Common;
using Grpc.Core;
using Plugin.Base;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Rpc.Service
{
    public class RpcServiceImpl: RpcService.RpcServiceBase
    {
        private IPlugin _plugin;

        public override Task<Empty> Update(Data data, ServerCallContext context)
        {
            throw new NotImplementedException();
        }

        public override async Task<Result> Query(Data data, ServerCallContext context)
        {
            try
            {
                var result = await _plugin.ExecuteOnAgent(data.Json);
                return new Result { Success = true, Message = "", Json = result != null ? result : "" };
            }
            catch (Exception ex)
            {
                return new Result { Success = false, Message = ex.Message };
            }
        }

        public override Task<Result> TestConnection(Empty empty, ServerCallContext context)
        {
            Log.Information($"Host {context.Host} connected");
            return Task.FromResult(new Result { Success = true, Message = "" });
        }

        public override Task<Result> InstallPlugin(Data data, ServerCallContext context)
        {
            Log.Information($"Install plugin '{data.Json}' ...");
            try
            {
                var type = Type.GetType(data.Json);
                _plugin = (IPlugin)Activator.CreateInstance(type);
                return Task.FromResult(new Result { Success = true, Message = "" });
            }
            catch (Exception ex)
            {
                var message = $"Fail to install plugin: {ex}";
                Log.Error(message);
                return Task.FromResult(new Result { Success = false, Message = message });
            }
        }
    }
}
