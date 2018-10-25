using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using Newtonsoft.Json;
using Plugin.Base;
using Serilog;
using Common;

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
            var parameters = _plugin.Deserialize(data.Json);

            // Display configurations
            var configuration = (from entry in parameters select $"  {entry.Key} : {entry.Value}").Aggregate((a, b) => a + Environment.NewLine + b);
            Log.Information($"Configuration:{Environment.NewLine}{configuration}");

            // Extract method name
            parameters.TryGetTypedValue(Constants.Method, out string method, Convert.ToString);

            // Create Instance
            ISlaveMethod methodInstance = _plugin.CreateSlaveMethodInstance(method);

            // Do action
            try
            {
                var result = await methodInstance.Do(parameters, _plugin.PluginSlaveParamaters);
                return new Result { Success = true, Message = "", Json = _plugin.Serialize(result)};
            }
            catch (Exception ex)
            {
                var message = $"Perform method '{method}' fail:{Environment.NewLine} {ex}";
                return new Result { Success = false, Message = message };
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
