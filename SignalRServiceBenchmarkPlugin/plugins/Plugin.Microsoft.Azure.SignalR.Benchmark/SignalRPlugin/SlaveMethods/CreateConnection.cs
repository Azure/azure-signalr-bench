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

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public class CreateConnection : ISlaveMethod
    {
        private int _closeTimeout = 100;

        public async Task<IDictionary<string, object>> Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Create connections...");

                // Get parameters
                stepParameters.TryGetTypedValue(SignalRConstants.ConnectionBegin, out int connectionBegin, Convert.ToInt32);
                stepParameters.TryGetTypedValue(SignalRConstants.ConnectionEnd, out int connectionEnd, Convert.ToInt32);
                stepParameters.TryGetTypedValue(SignalRConstants.HubUrl, out string url, Convert.ToString);
                stepParameters.TryGetTypedValue(SignalRConstants.HubProtocol, out string protocol, Convert.ToString);
                stepParameters.TryGetTypedValue(SignalRConstants.TransportType, out string transportType, Convert.ToString);
                stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
                // DEBUG
                stepParameters.TryGetTypedValue("ccc", out string xxx, Convert.ToString);

                // Create Connections
                var connections = CreateConnections(connectionEnd - connectionBegin, url, transportType, protocol);

                // Prepare plugin parameters
                pluginParameters[$"{SignalRConstants.ConnectionStore}.{type}"] = connections;
                pluginParameters[$"{SignalRConstants.ConnectionOffset}.{type}"] = connectionBegin;

                return null;
            }
            catch (Exception ex)
            {
                var message = $"Fail to create connections: {ex}";
                Log.Error(message);
                throw;
            }
        }

        private IList<HubConnection> CreateConnections(int total, string url, string transportTypeString, string protocolString)
        {
            var success = true;

            success = Enum.TryParse<HttpTransportType>(transportTypeString, true, out var transportType);
            PluginUtils.HandleParseEnumResult(success, transportTypeString);

            var connections = from i in Enumerable.Range(0, total)
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
                                      httpConnectionOptions.CloseTimeout = TimeSpan.FromMinutes(_closeTimeout);
                                      httpConnectionOptions.Cookies = cookies;
                                  })
                              select
                              protocolString.ToLower() == "messagepack" ? hubConnectionBuilder.AddMessagePackProtocol().Build() : hubConnectionBuilder.Build();

            return connections.ToList();
        }

    }
}
