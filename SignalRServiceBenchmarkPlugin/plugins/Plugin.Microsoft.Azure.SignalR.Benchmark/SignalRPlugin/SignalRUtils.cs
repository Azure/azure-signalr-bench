using Common;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Plugin.Base;
using Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods.Statistics;
using Rpc.Service;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using static Plugin.Microsoft.Azure.SignalR.Benchmark.SignalREnums;
using AspNetHubConnection = Microsoft.AspNet.SignalR.Client.HubConnection;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    public static class SignalRUtils
    {
        public static string GroupName(string type, int index) => $"{type}:{index}";

        public static string MessageLessThan(long latency) => $"message:lt:{latency}";

        public static string MessageGreaterOrEqaulTo(long latency) => $"message:ge:{latency}";

        public static Task MasterCreateConnection(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters, IList<IRpcClient> clients)
        {
            // Get parameters
            stepParameters.TryGetTypedValue(SignalRConstants.ConnectionTotal,
                out int connectionTotal, Convert.ToInt32);
            stepParameters.TryGetTypedValue(SignalRConstants.HubUrls,
                out string hubUrl, Convert.ToString);
            stepParameters.TryGetTypedValue(SignalRConstants.TransportType,
                out string transportType, Convert.ToString);
            stepParameters.TryGetTypedValue(SignalRConstants.HubProtocol,
                out string hubProtocol, Convert.ToString);

            // Shuffle connection indexes
            var indexes = Enumerable.Range(0, connectionTotal).ToList();
            indexes.Shuffle();

            // Prepare configuration for each clients
            var packages = clients.Select((client, i) =>
            {
                (int beg, int end) = Util.GetConnectionRange(connectionTotal, i, clients.Count);
                var data = new Dictionary<string, object>
                {
                    { SignalRConstants.HubUrls, hubUrl },
                    { SignalRConstants.TransportType, transportType },
                    { SignalRConstants.HubProtocol, hubProtocol },
                    { SignalRConstants.ConnectionIndex, string.Join(',', indexes.GetRange(beg, end - beg)) }
                };
                // Add method and type
                PluginUtils.AddMethodAndType(data, stepParameters);
                return new { Client = client, Data = data };
            });

            // Process on clients
            var results = from package in packages select package.Client.QueryAsync(package.Data);
            return Task.WhenAll(results);
        }

        public static void MarkConnectionType(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters,
            ClientType clientType)
        {
            stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
            pluginParameters[$"{SignalRConstants.ConnectionType}.{type}"] = clientType.ToString();
        }

        public static IList<IHubConnectionAdapter> CreateClientConnection(
            string transportType,
            string protocol,
            string urls,
            List<int> connectionIndex,
            ClientType clientType)
        {
            IList<IHubConnectionAdapter> connections = null;
            // Create Connections
            switch (clientType)
            {
                case ClientType.AspNet:
                    connections = CreateAspNetConnections(connectionIndex, urls, transportType,
                        protocol, SignalRConstants.ConnectionCloseTimeout);
                    break;
                case ClientType.DirectConnect:
                    connections = CreateDirectConnections(connectionIndex, urls, transportType,
                        protocol, SignalRConstants.ConnectionCloseTimeout);
                    break;
                default:
                    connections = CreateConnections(connectionIndex, urls, transportType,
                        protocol, SignalRConstants.ConnectionCloseTimeout);
                    break;
            }
            return connections;
        }

        public static Task<IDictionary<string, object>> SlaveCreateConnection(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters,
            ClientType clientType)
        {
            // Get parameters
            // Get parameters
            stepParameters.TryGetTypedValue(SignalRConstants.HubUrls,
                out string urls, Convert.ToString);
            stepParameters.TryGetTypedValue(SignalRConstants.HubProtocol,
                out string protocol, Convert.ToString);
            stepParameters.TryGetTypedValue(SignalRConstants.TransportType,
                out string transportType, Convert.ToString);
            stepParameters.TryGetTypedValue(SignalRConstants.Type,
                out string type, Convert.ToString);
            stepParameters.TryGetTypedValue(SignalRConstants.ConnectionIndex,
                out string connectionIndexString, Convert.ToString);

            var connectionIndex = connectionIndexString.Split(',').Select(ind => Convert.ToInt32(ind)).ToList();
            // Create Connections
            var connections = CreateClientConnection(transportType, protocol, urls, connectionIndex, clientType);
            // Setup connection success flag
            var connectionsSuccessFlag = Enumerable.Repeat(ConnectionState.Init, connections.Count()).ToList();

            // Setup connection drop handler
            SetConnectionOnClose(connections, connectionsSuccessFlag);

            // Prepare plugin parameters
            pluginParameters[$"{SignalRConstants.ConnectionStore}.{type}"] = connections;
            pluginParameters[$"{SignalRConstants.ConnectionIndex}.{type}"] = connectionIndex;
            pluginParameters[$"{SignalRConstants.ConnectionSuccessFlag}.{type}"] = connectionsSuccessFlag;
            pluginParameters[$"{SignalRConstants.RegisteredCallbacks}.{type}"] =
                new List<Action<IList<IHubConnectionAdapter>, StatisticsCollector, string>>();
            // record the client type for reconnect
            MarkConnectionType(stepParameters, pluginParameters, clientType);
            if (clientType == ClientType.DirectConnect)
            {
                // record the connection string for REST API send
                pluginParameters[$"{SignalRConstants.ConnectionString}.{type}"] = urls;
            }
            return Task.FromResult<IDictionary<string, object>>(null);
        }

        public static async Task StartConnect((IHubConnectionAdapter Connection, int LocalIndex,
            List<ConnectionState> connectionsSuccessFlag,
            ConnectionState NormalState,
            ConnectionState AbnormalState) package)
        {
            if (package.Connection == null ||
                package.connectionsSuccessFlag[package.LocalIndex] != ConnectionState.Init)
                return;

            try
            {
                var c = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                await package.Connection.StartAsync(c.Token);
                package.connectionsSuccessFlag[package.LocalIndex] = package.NormalState;
            }
            catch (Exception ex)
            {
                package.connectionsSuccessFlag[package.LocalIndex] = package.AbnormalState;
                var message = $"Fail to start connection: {ex}";
                // only record error instead of throwing exception, allow reconnect
                Log.Error(message);
            }
        }

        public static string GenClientUserIdFromConnectionIndex(int connectionIndex)
        {
            return $"{SignalRConstants.DefaultClientUserIdPrefix}{connectionIndex}";
        }

        public static IList<IHubConnectionAdapter> CreateDirectConnections(
            IList<int> connectionIndex,
            string connectionString,
            string transportTypeString,
            string protocolString,
            int closeTimeout)
        {
            var transportType = GetTransportType(transportTypeString);
            var restApi = new RestApiProvider(connectionString, SignalRConstants.DefaultRestHubName);
            var clientUrl = restApi.GetClientUrl();
            var audience = restApi.GetClientAudience();
            var connections = from i in Enumerable.Range(0, connectionIndex.Count)
                              let cookies = new CookieContainer()
                              let handler = new HttpClientHandler
                              {
                                  ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                                  CookieContainer = cookies,
                              }
                              let userId = GenClientUserIdFromConnectionIndex(connectionIndex[i])
                              select new HubConnectionBuilder()
                              .ConfigureLogging(logger =>
                              {
                                  logger.ClearProviders();
                                  logger.AddSerilog(dispose: true);
                                  logger.SetMinimumLevel(LogLevel.Error);
                              })
                              .WithUrl(clientUrl, httpConnectionOptions =>
                              {
                                  httpConnectionOptions.HttpMessageHandlerFactory = _ => handler;
                                  httpConnectionOptions.Transports = transportType;
                                  httpConnectionOptions.CloseTimeout = TimeSpan.FromMinutes(closeTimeout);
                                  httpConnectionOptions.Cookies = cookies;
                                  httpConnectionOptions.AccessTokenProvider = () =>
                                  {
                                      return Task.FromResult(restApi.GenerateAccessToken(audience, userId));
                                  };
                              }) into builder
                              let hubConnection = protocolString.ToLower() == "messagepack" ?
                                                  builder.AddMessagePackProtocol().Build() : builder.Build()
                              select (IHubConnectionAdapter)(new SignalRCoreHubConnection(hubConnection));

            return connections.ToList();
        }

        public static IList<IHubConnectionAdapter> CreateAspNetConnections(
            IList<int> connectionIndex,
            string urls,
            string transportTypeString,
            string protocolString,
            int closeTimeout)
        {
            List<string> urlList = urls.Split(',').ToList();
            var connections = from i in Enumerable.Range(0, connectionIndex.Count)
                              let url = urlList[connectionIndex[i] % urlList.Count()]
                              let path = url.Substring(0, url.LastIndexOf('/'))
                              let hubName = url.Substring(url.LastIndexOf('/') + 1)
                              let hubConnection = new AspNetHubConnection(path)
                              select (IHubConnectionAdapter)(new AspNetSignalRHubConnection(hubConnection, hubName, transportTypeString));
            return connections.ToList();
        }

        private static HttpTransportType GetTransportType(string transportTypeString)
        {
            var success = Enum.TryParse<HttpTransportType>(transportTypeString, true, out var transportType);
            if (!success)
            {
                var message = $"Fail to parse enum '{transportTypeString}'.";
                Log.Error(message);
                throw new Exception(message);
            }
            return transportType;
        }

        public static IList<IHubConnectionAdapter> CreateConnections(
            IList<int> connectionIndex,
            string urls,
            string transportTypeString,
            string protocolString,
            int closeTimeout)
        {
            var transportType = GetTransportType(transportTypeString);
            List<string> urlList = urls.Split(',').ToList();

            var connections = from i in Enumerable.Range(0, connectionIndex.Count)
                              let cookies = new CookieContainer()
                              let handler = new HttpClientHandler
                              {
                                  ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                                  CookieContainer = cookies,
                              }
                              select new HubConnectionBuilder()
                              .WithUrl(urlList[connectionIndex[i] % urlList.Count()], httpConnectionOptions =>
                              {
                                  httpConnectionOptions.HttpMessageHandlerFactory = _ => handler;
                                  httpConnectionOptions.Transports = transportType;
                                  httpConnectionOptions.CloseTimeout = TimeSpan.FromMinutes(closeTimeout);
                                  httpConnectionOptions.Cookies = cookies;
                              }) into builder
                              let hubConnection = protocolString.ToLower() == "messagepack" ?
                                                  builder.AddMessagePackProtocol().Build() : builder.Build()
                              select (IHubConnectionAdapter)(new SignalRCoreHubConnection(hubConnection));

            return connections.ToList();
        }

        public static void DumpConnectionStatus(IList<SignalREnums.ConnectionState> connectionsSuccessFlag)
        {
            int success = 0;
            int failed = 0;
            int init = 0;
            for (var i = 0; i < connectionsSuccessFlag.Count; i++)
            {
                if (connectionsSuccessFlag[i] == SignalREnums.ConnectionState.Success)
                {
                    success++;
                }
                if (connectionsSuccessFlag[i] == SignalREnums.ConnectionState.Fail)
                {
                    failed++;
                }
                if (connectionsSuccessFlag[i] == SignalREnums.ConnectionState.Init)
                {
                    init++;
                }
            }
            Log.Information($"Connection status: success: {success}, failed: {failed}, init: {init}");
        }

        public static void SetConnectionOnClose(
            IList<IHubConnectionAdapter> connections,
            IList<ConnectionState> connectionsSuccessFlag)
        {
            // Setup connection drop handler
            for (var i = 0; i < connections.Count(); i++)
            {
                var index = i;
                connections[i].Closed += e =>
                {
                    connectionsSuccessFlag[index] = ConnectionState.Fail;
                    // AspNet SignalR does not pass exception object
                    if (e != null)
                    {
                        Log.Error($"connection closed for {e.Message}");
                    }
                    else
                    {
                        Log.Error("connection closed");
                    }
                    return Task.CompletedTask;
                };
            }
        }

        // Why shall we prefer 'string' rather than 'byte[]' data type?
        // messagepack protocol supports both string and byte[], json protocol only supports string data type.
        // So, if we define the data type is byte[], when using json protocol, there are potential issues,
        // for example, non-ASCII data will miss. In addition, SignalR will convert byte[] to string, as a result,
        // the data size changes without any notification.
        public static string GenerateRandomData(int len)
        {
            var message = new byte[len];
            Random rnd = new Random();
            rnd.NextBytes(message);
            return Convert.ToBase64String(message);
        }

        private static long EvaluatePayloadSize(IDictionary<string, object> payload)
        {
            long sz = 0;
            if (payload.ContainsKey(SignalRConstants.MessageBlob))
            {
                payload.TryGetValue(SignalRConstants.MessageBlob, out var messageBlob);

                if (messageBlob.GetType() == typeof(string))
                {
                    var array = (string)messageBlob;
                    sz += array.Length;
                }
                else if (messageBlob.GetType() == typeof (byte[]))
                {
                    var array = (byte[])messageBlob;
                    sz += array.Length;
                }
            }
            if (payload.ContainsKey(SignalRConstants.Timestamp))
            {
                sz += sizeof(long);
            }
            if (payload.ContainsKey(SignalRConstants.ConnectionId))
            {
                payload.TryGetValue(SignalRConstants.ConnectionId, out var connId);
                sz += ((string)connId).Length;
            }
            if (payload.ContainsKey(SignalRConstants.GroupName))
            {
                payload.TryGetValue(SignalRConstants.GroupName, out var grpName);
                sz += ((string)grpName).Length;
            }
            return sz;
        }

        public static void RecordSend(
            IDictionary<string, object> payload,
            StatisticsCollector StatisticsCollector)
        {
            var sz = EvaluatePayloadSize(payload);
            StatisticsCollector.IncreaseSendSize(sz);
            StatisticsCollector.IncreaseSentMessage();
        }

        public static void RecordRecvSize(
            IDictionary<string, object> payload,
            StatisticsCollector StatisticsCollector)
        {
            var sz = EvaluatePayloadSize(payload);
            StatisticsCollector.IncreaseRecvSize(sz);
        }

        public static IDictionary<string, long> MergeStatistics(
            IDictionary<string, object>[] results,
            string type, long latencyMax, long latencyStep)
        {
            var merged = new Dictionary<string, long>();

            // Sum of connection statistics
            merged[SignalRConstants.StatisticsConnectionConnectSuccess] = Sum(results, SignalRConstants.StatisticsConnectionConnectSuccess);
            merged[SignalRConstants.StatisticsConnectionConnectFail] = Sum(results, SignalRConstants.StatisticsConnectionConnectFail);
            merged[SignalRConstants.StatisticsConnectionReconnect] = Sum(results, SignalRConstants.StatisticsConnectionReconnect);
            //merged[SignalRConstants.StatisticsConnectionInit] = Sum(results, SignalRConstants.StatisticsConnectionInit);

            // Sum of group statistics
            merged[SignalRConstants.StatisticsGroupJoinSuccess] = Sum(results, SignalRConstants.StatisticsGroupJoinSuccess);
            merged[SignalRConstants.StatisticsGroupJoinFail] = Sum(results, SignalRConstants.StatisticsGroupJoinFail);
            merged[SignalRConstants.StatisticsGroupLeaveSuccess] = Sum(results, SignalRConstants.StatisticsGroupLeaveSuccess);
            merged[SignalRConstants.StatisticsGroupLeaveFail] = Sum(results, SignalRConstants.StatisticsGroupLeaveFail);

            // Sum of "message:lt:latency"
            var SumMessageLatencyStatistics = (from i in Enumerable.Range(1, (int)latencyMax / (int)latencyStep)
                                               let latency = i * latencyStep
                                               select new { Key = MessageLessThan(latency), Sum = Sum(results, MessageLessThan(latency)) })
                                               .ToDictionary(entry => entry.Key, entry => entry.Sum);
            // Sum of "message:ge:latency"
            SumMessageLatencyStatistics[MessageGreaterOrEqaulTo(latencyMax)] = Sum(results, MessageGreaterOrEqaulTo(latencyMax));

            // Sum of total received message count
            merged[SignalRConstants.StatisticsMessageReceived] = SumMessageLatencyStatistics.Select(entry => entry.Value).Sum();

            // Sum of sent message statistics (should be calculated after "message:ge:latency")
            merged[SignalRConstants.StatisticsMessageSent] = Sum(results, SignalRConstants.StatisticsMessageSent);
            merged[SignalRConstants.StatisticsMessageSentSize] = Sum(results, SignalRConstants.StatisticsMessageSentSize);
            merged[SignalRConstants.StatisticsMessageReceivedSize] = Sum(results, SignalRConstants.StatisticsMessageReceivedSize);

            // Update epoch
            merged[SignalRConstants.StatisticsEpoch] = Min(results, SignalRConstants.StatisticsEpoch);
            merged[SignalRConstants.StatisticsSendingStep] = Min(results, SignalRConstants.StatisticsSendingStep);
            merged = merged.Union(SumMessageLatencyStatistics).ToDictionary(entry => entry.Key, entry => entry.Value);

            return merged;
        }

        public static bool TryGetBatchMode(
            IDictionary<string, object> stepParameters,
            out string batchConfigMode,
            out int batchWaitMilliSeconds,
            out SignalREnums.BatchMode mode)
        {
            batchConfigMode = SignalRConstants.DefaultBatchMode;
            batchWaitMilliSeconds = SignalRConstants.BatchProcessDefaultWait;
            if (stepParameters.TryGetValue(SignalRConstants.BatchMode, out _))
            {
                stepParameters.TryGetTypedValue(SignalRConstants.BatchMode,
                    out string batchMode, Convert.ToString);
                batchConfigMode = batchMode;
            }
            if (stepParameters.TryGetValue(SignalRConstants.BatchWait, out _))
            {
                stepParameters.TryGetTypedValue(SignalRConstants.BatchWait,
                    out int batchWait, Convert.ToInt32);
                batchWaitMilliSeconds = batchWait;
            }
            if (!Enum.TryParse(batchConfigMode, out SignalREnums.BatchMode m))
            {
                var message = $"Config mode not supported: {batchConfigMode}";
                Log.Error(message);
                throw new Exception(message);
            }
            mode = m;
            return true;
        }

        private static long Sum(IDictionary<string, object>[] results, string key)
        {
            return results.ToList().Select(statistics =>
            {
                if (statistics.ContainsKey(key))
                {
                    statistics.TryGetTypedValue(key, out long item, Convert.ToInt64);
                    return item;
                }
                return 0;
            }).Sum();
        }

        private static long Min(IDictionary<string, object>[] results, string key)
        {
            return results.ToList().Select(statistics =>
            {
                if (statistics.ContainsKey(key))
                {
                    statistics.TryGetTypedValue(key, out long item, Convert.ToInt64);
                    return item;
                }
                return 0;
            }).Min();
        }

        public static void ResetCounters(StatisticsCollector statisticsCollecter)
        {
            statisticsCollecter.ResetGroupCounters();
            statisticsCollecter.ResetMessageCounters();
            statisticsCollecter.ResetReconnectCounters();
        }
    }
}
