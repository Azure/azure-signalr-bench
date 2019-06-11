using Plugin.Base;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Plugin.Microsoft.Azure.SignalR.Benchmark.SignalREnums;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public class CreateDirectConnection : ISlaveMethod
    {
        public async Task<IDictionary<string, object>> Do(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Create connections...");
                await SignalRUtils.StartNegotiationServer(stepParameters, pluginParameters);
                SignalRUtils.SlaveCreateConnection(stepParameters, pluginParameters, ClientType.DirectConnect);
                SignalRUtils.CreateHttpClientManagerAndSaveToContext(stepParameters, pluginParameters);
                return null;
            }
            catch (Exception ex)
            {
                var message = $"Fail to create connections: {ex}";
                Log.Error(message);
                throw;
            }
        }
    }
}
