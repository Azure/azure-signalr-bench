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
                stepParameters.TryGetTypedValue(SignalRConstants.HubUrls, out string urls, Convert.ToString);
                stepParameters.TryGetTypedValue(SignalRConstants.HubProtocol, out string protocol, Convert.ToString);
                stepParameters.TryGetTypedValue(SignalRConstants.TransportType, out string transportType, Convert.ToString);
                stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
                stepParameters.TryGetTypedValue(SignalRConstants.ConnectionIndex, out string connectionIndex, Convert.ToString);

                // Create Connections
                var connections = CreateConnections(connectionBegin, connectionEnd - connectionBegin, urls, transportType, protocol);

                // Setup connection success flag
                var connectionsSuccessFlag = Enumerable.Repeat(SignalREnums.ConnectionState.Init, connections.Count()).ToList();

                // Setup connection drop handler
                for (int i = 0; i < connections.Count(); i++)
                {
                    connections[i].Closed += e =>
                    {
                        connectionsSuccessFlag[i] = SignalREnums.ConnectionState.Fail;
                        return Task.CompletedTask;
                    };
                }

                // Prepare plugin parameters
                pluginParameters[$"{SignalRConstants.ConnectionStore}.{type}"] = connections;
                pluginParameters[$"{SignalRConstants.ConnectionOffset}.{type}"] = connectionBegin;
                pluginParameters[$"{SignalRConstants.ConnectionIndex}.{type}"] = connectionIndex.Split(',').Select(ind => Convert.ToInt32(ind)).ToList();
                pluginParameters[$"{SignalRConstants.ConnectionSuccessFlag}.{type}"] = connectionsSuccessFlag;

                return null;
            }
            catch (Exception ex)
            {
                var message = $"Fail to create connections: {ex}";
                Log.Error(message);
                throw;
            }
        }

        private IList<HubConnection> CreateConnections(int offset, int total, string urls, string transportTypeString, string protocolString)
        {
            var success = true;

            success = Enum.TryParse<HttpTransportType>(transportTypeString, true, out var transportType);
            PluginUtils.HandleParseEnumResult(success, transportTypeString);

            List<string> urlList = urls.Split(',').ToList();

            var connections = from i in Enumerable.Range(0, total)
                              let cookies = new CookieContainer()
                              let handler = new HttpClientHandler
                              {
                                  ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                                  CookieContainer = cookies,
                              }
                              select new HubConnectionBuilder()
                              .WithUrl(urlList[(i + offset) % urlList.Count()], httpConnectionOptions =>
                              {
                                  httpConnectionOptions.HttpMessageHandlerFactory = _ => handler;
                                  httpConnectionOptions.Transports = transportType;
                                  httpConnectionOptions.CloseTimeout = TimeSpan.FromMinutes(_closeTimeout);
                                  httpConnectionOptions.Cookies = cookies;
                              }) into builder
                              select protocolString.ToLower() == "messagepack" ? builder.AddMessagePackProtocol().Build() : builder.Build();

            return connections.ToList();
        }

    }
}
