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
            Log.Information($"Create connections...");

            // Get parameters
            var success = true;

            success = stepParameters.TryGetTypedValue(SignalRConstants.ConnectionBegin, out int connectionBegin, Convert.ToInt32);
            PluginUtils.HandleGetValueResult(success, SignalRConstants.ConnectionBegin);

            success = stepParameters.TryGetTypedValue(SignalRConstants.ConnectionEnd, out int connectionEnd, Convert.ToInt32);
            PluginUtils.HandleGetValueResult(success, SignalRConstants.ConnectionBegin);

            success = stepParameters.TryGetTypedValue(SignalRConstants.HubUrl, out string url, Convert.ToString);
            PluginUtils.HandleGetValueResult(success, SignalRConstants.HubUrl);

            success = stepParameters.TryGetTypedValue(SignalRConstants.HubProtocol, out string protocol, Convert.ToString);
            PluginUtils.HandleGetValueResult(success, SignalRConstants.HubProtocol);

            success = stepParameters.TryGetTypedValue(SignalRConstants.TransportType, out string transportType, Convert.ToString);
            PluginUtils.HandleGetValueResult(success, SignalRConstants.TransportType);

            success = stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
            PluginUtils.HandleGetValueResult(success, SignalRConstants.Type);

            // Create Connections
            var connections = CreateConnections(connectionEnd - connectionBegin, url, transportType, protocol);

            // Prepare plugin parameters
            pluginParameters[$"{SignalRConstants.ConnectionStore}.{type}"] = connections;
            pluginParameters[$"{SignalRConstants.ConnectionOffset}.{type}"] = connectionBegin;
        }

        private IList<HubConnection> CreateConnections(int total, string url, string transportTypeString, string protocolString)
        {
            try
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
                protocolString == "json" ? hubConnectionBuilder.Build() : hubConnectionBuilder.AddMessagePackProtocol().Build();

                return connections.ToList();
            }
            catch (Exception ex)
            {
                var message = $"Fail to create connections: {ex}";
                Log.Error(message);
                throw new Exception(message);
            }
        }

    }
}
