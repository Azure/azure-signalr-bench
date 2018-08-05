using Bench.Common;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Bench.RpcSlave.Worker.Operations
{
    class CreateConnOp : BaseOp, IOperation
    {
        private WorkerToolkit _tk;
        public async Task Do(WorkerToolkit tk)
        {
            _tk = tk;

            Util.Log($"server url: {_tk.JobConfig.ServerUrl}; conn: {_tk.JobConfig.Connections};  _tk.BenchmarkCellConfig.TransportType: { _tk.BenchmarkCellConfig.TransportType}; _tk.BenchmarkCellConfig.HubProtocol: {_tk.BenchmarkCellConfig.HubProtocol}");
            _tk.State = Stat.Types.State.HubconnUnconnected;
            _tk.Connections = Create(_tk.ConnectionRange.End - _tk.ConnectionRange.Begin, _tk.JobConfig.ServerUrl, _tk.BenchmarkCellConfig.TransportType, _tk.BenchmarkCellConfig.HubProtocol);
            if (tk.Connections == null) Util.Log("connections == null");
            Util.Log($"xxxconnections: {_tk.Connections.Count}");
        }

        private List<HubConnection> Create(int conn, string url,
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

            _tk.State = Stat.Types.State.HubconnCreating;
            var connections = new List<HubConnection>(conn);
            for (var i = 0; i < conn; i++)
            {
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
                    .WithUrl(url, httpConnectionOptions =>
                    {
                        httpConnectionOptions.HttpMessageHandlerFactory = _ => httpClientHandler;
                        httpConnectionOptions.Transports = transportType;
                        httpConnectionOptions.CloseTimeout = TimeSpan.FromMinutes(100);
                        httpConnectionOptions.Cookies = cookies;
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
                connections.Add(connection);
            }

            _tk.State = Stat.Types.State.HubconnCreated;
            return connections;

        }
    }
}
