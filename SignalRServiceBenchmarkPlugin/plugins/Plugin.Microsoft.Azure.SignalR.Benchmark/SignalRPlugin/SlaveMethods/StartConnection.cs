using Microsoft.AspNetCore.SignalR.Client;
using Plugin.Base;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public class StartConnection : ISlaveMethod
    {
        public async Task Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Start connections...");

                // Get parameters
                PluginUtils.TryGetTypedValue(stepParameters, SignalRConstants.ConcurrentConnection, out int concurrentConnection, Convert.ToInt32);
                PluginUtils.TryGetTypedValue(stepParameters, SignalRConstants.Type, out string type, Convert.ToString);
                PluginUtils.TryGetTypedValue(pluginParameters, SignalRConstants.ConnectionStore, out IList<HubConnection> connections, (obj) => (IList<HubConnection>)obj);

                await StartConnections(connections, StartConnect, concurrentConnection);
            }
            catch (Exception ex)
            {
                var message = $"Fail to start connections: {ex}";
                Log.Error(message);
                throw new Exception(message);
            }
        }

        private Task StartConnections<T>(IList<T> source, Func<T, Task> f, int max)
        {
            var initial = (max >> 1);
            var s = new System.Threading.SemaphoreSlim(initial, max);
            _ = Task.Run(async () =>
            {
                for (int i = initial; i < max; i++)
                {
                    await Task.Delay(100);
                    s.Release();
                }
            });

            return Task.WhenAll(from item in source
                                select Task.Run( async () =>
                                {
                                    await s.WaitAsync();
                                    try
                                    {
                                        await f(item);
                                    }
                                    finally
                                    {
                                        s.Release();
                                    }
                                    
                                }));
        }

        private async Task StartConnect(HubConnection connection)
        {
            if (connection == null) return;
            try
            {
                await connection.StartAsync();
            }
            catch (Exception ex)
            {
                var message = $"Fail to start connection: {ex}";
                Log.Error(message);
                throw new Exception(message);
            }
        }
    }
}
