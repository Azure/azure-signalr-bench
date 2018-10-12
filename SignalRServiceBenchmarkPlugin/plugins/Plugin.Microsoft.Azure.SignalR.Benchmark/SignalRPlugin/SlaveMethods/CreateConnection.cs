using Common;
using Plugin.Base;
using Rpc.Service;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethod
{
    public class CreateConnection : ISlaveMethod
    {
        public async Task Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Create connections...");

                // Get parameters
                PluginUtils.TryGetTypedValue(stepParameters, SignalRConstants.ConnectionBegin, out int connectionBegin, Convert.ToInt32);
                PluginUtils.TryGetTypedValue(stepParameters, SignalRConstants.ConnectionEnd, out int connectionEnd, Convert.ToInt32);
                PluginUtils.TryGetTypedValue(stepParameters, SignalRConstants.HubUrl, out string url, Convert.ToString);
                PluginUtils.TryGetTypedValue(stepParameters, SignalRConstants.HubProtocol, out string protocol, Convert.ToString);
                PluginUtils.TryGetTypedValue(stepParameters, SignalRConstants.TransportType, out string transportType, Convert.ToString);
                PluginUtils.TryGetTypedValue(stepParameters, SignalRConstants.Type, out string type, Convert.ToString);

                // Create Connections
                var connections = CreateConnections(connectionEnd - connectionBegin, url, transportType, protocol);

                // Prepare plugin parameters
                pluginParameters[$"{SignalRConstants.ConnectionStore}.{type}"] = connections;
                pluginParameters[$"{SignalRConstants.ConnectionOffset}.{type}"] = connectionBegin;

            }
            catch (Exception ex)
            {
                var message = $"Fail to create connections: {ex}";
                Log.Error(message);
                throw new Exception(message);
            }
        }

        private IList<HubConnection> CreateConnections(int total, string url, string transportTypeString, string protocolString)
        {
            var success = true;

            success = Enum.TryParse<HttpTransportType>(transportTypeString, true, out var transportType);
            PluginUtils.HandleParseEnumResult(success, transportTypeString);

            var connections = Enumerable.Repeat<HubConnection>(null, total);
            connections =
            from connection in connections
            let cookies = new CookieContainer()
            let httpClientHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                CookieContainer = cookies,
            }
            let hubConnectionBuilder = new HubConnectionBuilder()
                .WithUrl(url, httpConnectionOptions =>
                {
                    httpConnectionOptions.HttpMessageHandlerFactory = _ => httpClientHandler;
                    httpConnectionOptions.Transports = transportType;
                    httpConnectionOptions.CloseTimeout = TimeSpan.FromMinutes(100);
                    httpConnectionOptions.Cookies = cookies;
                })
            select
            protocolString.ToLower() == "messagepack" ? hubConnectionBuilder.AddMessagePackProtocol().Build() : hubConnectionBuilder.Build();

            return connections.ToList();
        }

    }
}
