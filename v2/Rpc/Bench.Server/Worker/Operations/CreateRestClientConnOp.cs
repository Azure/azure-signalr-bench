using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Bench.Common;
using Bench.RpcSlave.Worker.Serverless;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bench.RpcSlave.Worker.Operations
{
    class CreateRestClientConnOp : BaseOp, IOperation
    {
        private WorkerToolkit _tk;

        public Task Do(WorkerToolkit tk)
        {
            _tk = tk;
            // here url is connectionString, we borrow it for not changing RPC model
            Util.Log($"server url: {_tk.JobConfig.ServerUrl}; conn: {_tk.JobConfig.Connections};  _tk.BenchmarkCellConfig.TransportType: { _tk.BenchmarkCellConfig.TransportType}; _tk.BenchmarkCellConfig.HubProtocol: {_tk.BenchmarkCellConfig.HubProtocol}");
            _tk.State = Stat.Types.State.HubconnUnconnected;
            _tk.ConnectionString = _tk.JobConfig.ServerUrl;
            _tk.Connections = Create(_tk.ConnectionRange.Begin, _tk.ConnectionRange.End,
                _tk.JobConfig.ServerUrl, _tk.BenchmarkCellConfig.TransportType,
                _tk.BenchmarkCellConfig.HubProtocol);

            if (tk.Connections == null) Util.Log("connections == null");
            Util.Log($"xxxconnections: {_tk.Connections.Count}");
            return Task.CompletedTask;
        }

        private List<HubConnection> Create(int startCliIndex, int endCliIndex, string url,
            string transportTypeName = "Websockets",
            string hubProtocol = "json")
        {
            Util.Log($"transport type: {transportTypeName}");
            var transportType = HttpTransportType.WebSockets;
            switch (transportTypeName)
            {
                case "LongPolling":
                    transportType = HttpTransportType.LongPolling;
                    break;
                case "ServerSentEvents":
                    transportType = HttpTransportType.ServerSentEvents;
                    break;
                case "None":
                    transportType = HttpTransportType.None;
                    break;
                default:
                    transportType = HttpTransportType.WebSockets;
                    break;
            }
            Util.Log($"Connection string: {url}");
            var serviceUtils = new ServiceUtils(url);

            _tk.State = Stat.Types.State.HubconnCreating;
            var connections = new List<HubConnection>(endCliIndex - startCliIndex);
            for (var i = startCliIndex; i < endCliIndex; i++)
            {
                var serviceUrl = serviceUtils.GetClientUrl();
                var userId = $"{ServiceUtils.ClientUserIdPrefix}{i}";

                var cookies = new CookieContainer();
                var httpClientHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                    CookieContainer = cookies,
                };
                var hubConnectionBuilder = new HubConnectionBuilder()
                    /* TODO. Console log is important for finding errors.
                     * But if every connection enables it, there will be thousands of
                     * 'Console logger queue processing thread' which degrade the system
                     * response, and bring issues to counters statistic.
                     * Temporarily, we disable it. We need to find the best way
                     * to enable it.
                     */
                    //.ConfigureLogging(logging =>
                    //{
                    //    logging.AddConsole();
                    //    logging.SetMinimumLevel(LogLevel.Warning);
                    //})
                    .WithUrl(serviceUrl, httpConnectionOptions =>
                    {
                        httpConnectionOptions.HttpMessageHandlerFactory = _ => httpClientHandler;
                        httpConnectionOptions.Transports = transportType;
                        httpConnectionOptions.CloseTimeout = TimeSpan.FromMinutes(100);
                        httpConnectionOptions.Cookies = cookies;
                        httpConnectionOptions.AccessTokenProvider = () =>
                        {
                            return Task.FromResult(serviceUtils.GenerateAccessToken(serviceUrl, userId));
                        };
                    });

                HubConnection connection = null;
                switch (hubProtocol)
                {
                    case "json":
                        connection = hubConnectionBuilder.Build();
                        break;
                    case "messagepack":
                        connection = hubConnectionBuilder.AddMessagePackProtocol().Build();
                        break;
                    default:
                        throw new Exception($"{hubProtocol} is invalid.");
                }

                connection.Closed += e =>
                {
                    if (_tk.State <= Stat.Types.State.SendComplete && _tk.State >= Stat.Types.State.SendReady)
                    {
                        var error = $"Connection closed early: {e}";
                        Util.Log(error);
                    }

                    return Task.CompletedTask;
                };
                connection.On<string, string>(ServiceUtils.MethodName,
                    (string server, string timestamp) =>
                    {
                        var receiveTimestamp = Util.Timestamp();
                        var sendTimestamp = Convert.ToInt64(timestamp);
                        _tk.Counters.CountLatency(sendTimestamp, receiveTimestamp);
                    });
                connections.Add(connection);
            }

            _tk.State = Stat.Types.State.HubconnCreated;
            return connections;
        }
    }
}
