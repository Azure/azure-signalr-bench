using Bench.Common;
using Bench.RpcSlave.Worker.Serverless;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Bench.RpcSlave.Worker.Rest
{
    public class ConnectionUtils
    {
        public static string ComposeRestUserId(int index)
        {
            return $"{ServiceUtils.ClientUserIdPrefix}{index}";
        }

        public static bool IsRestUserDropped(WorkerToolkit tk, string userId)
        {
            var prefix = ServiceUtils.ClientUserIdPrefix.Count();
            var isNumeric = int.TryParse(userId.Substring(prefix), out int n);
            if (isNumeric)
            {
                return tk.BrokenConnectionTrackList[n] != -1;
            }
            return false;
        }

        public static void CreateBrokenConnectionTrackList(WorkerToolkit tk, int count)
        {
            tk.BrokenConnectionTrackList = Enumerable.Repeat(-1, count).ToList();
        }

        public static void ResetBrokenConnectionTrackList(WorkerToolkit tk)
        {
            if (tk.BrokenConnectionTrackList != null)
            {
                tk.BrokenConnectionTrackList.ForEach(i => i = -1);
            }
        }

        public static HubConnection CreateSingleDirectConnection(WorkerToolkit tk, string connectionString, int i)
        {
            var transportTypeName = tk.BenchmarkCellConfig.TransportType;
            var hubProtocol = tk.BenchmarkCellConfig.HubProtocol;
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

            var serviceUtils = new ServiceUtils(connectionString);
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
                if (tk.State <= Stat.Types.State.SendComplete && tk.State >= Stat.Types.State.SendReady)
                {
                    var error = $"Connection closed early: {e}";
                    Util.Log(error);
                    tk.BrokenConnectionTrackList[i] = i;
                }

                return Task.CompletedTask;
            };
            return connection;
        }

        public static HubConnection CreateSingleConnection(WorkerToolkit tk, int i)
        {
            var serverUrls = tk.JobConfig.ServerUrl.Split(';');
            var transportTypeName = tk.BenchmarkCellConfig.TransportType;
            var hubProtocol = tk.BenchmarkCellConfig.HubProtocol;
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
                .WithUrl(serverUrls[i % serverUrls.Length], httpConnectionOptions =>
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
                if (tk.State <= Stat.Types.State.SendComplete && tk.State >= Stat.Types.State.SendReady)
                {
                    var error = $"Connection closed early: {e}";
                    Util.Log(error);
                    tk.BrokenConnectionTrackList[i] = i;
                }

                return Task.CompletedTask;
            };
            return connection;
        }

        public static async Task<bool> StartConnection(WorkerToolkit tk, int i)
        {
            var connection = tk.Connections[i];
            try
            {
                await connection.StartAsync();
                tk.Counters.IncreaseConnectionSuccess();
                return true;
            }
            catch (Exception ex)
            {
                Util.Log($"start connection exception: {ex}");
                tk.Counters.IncreaseConnectionError();
            }
            return false;
        }
    }
}
