using Common;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Plugin.Base;
using Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods.Statistics;
using Rpc.Service;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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

        public static Task<IDictionary<string, object>> SlaveCreateConnection(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters,
            ClientType clientType)
        {
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
            IList<IHubConnectionAdapter> connections;
            if (clientType == ClientType.AspNetCore)
            {
                connections = CreateConnections(connectionIndex, urls,
                    transportType, protocol, SignalRConstants.ConnectionCloseTimeout);
            }
            else
            {
                connections = CreateAspNetConnections(connectionIndex, urls,
                    transportType, protocol, SignalRConstants.ConnectionCloseTimeout);
            }

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
                await package.Connection.StartAsync();
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

        public static IList<IHubConnectionAdapter> CreateAspNetConnections(IList<int> connectionIndex,
            string urls, string transportTypeString, string protocolString, int closeTimeout)
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

        public static IList<IHubConnectionAdapter> CreateConnections(IList<int> connectionIndex,
            string urls, string transportTypeString, string protocolString, int closeTimeout)
        {
            var success = true;

            success = Enum.TryParse<HttpTransportType>(transportTypeString, true, out var transportType);
            PluginUtils.HandleParseEnumResult(success, transportTypeString);

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
                    return Task.CompletedTask;
                };
            }
        }

        public static IDictionary<string, int> MergeStatistics(
            IDictionary<string, object>[] results,
            string type, long latencyMax, long latencyStep)
        {
            var merged = new Dictionary<string, int>();

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
                                               select new { Key = MessageLessThan(latency), Sum = Sum(results, MessageLessThan(latency)) }).ToDictionary(entry => entry.Key, entry => entry.Sum);
            // Sum of "message:ge:latency"
            SumMessageLatencyStatistics[MessageGreaterOrEqaulTo(latencyMax)] = Sum(results, MessageGreaterOrEqaulTo(latencyMax));

            // Sum of total received message count
            merged[SignalRConstants.StatisticsMessageReceived] = SumMessageLatencyStatistics.Select(entry => entry.Value).Sum();

            // Sum of sent message statistics (should be calculated after "message:ge:latency")
            merged[SignalRConstants.StatisticsMessageSent] = Sum(results, SignalRConstants.StatisticsMessageSent);

            // Update epoch
            merged[SignalRConstants.StatisticsEpoch] = Min(results, SignalRConstants.StatisticsEpoch);
            merged[SignalRConstants.StatisticsSendingStep] = Min(results, SignalRConstants.StatisticsSendingStep);
            merged = merged.Union(SumMessageLatencyStatistics).ToDictionary(entry => entry.Key, entry => entry.Value);

            return merged;
        }

        private static int Sum(IDictionary<string, object>[] results, string key)
        {
            return results.ToList().Select(statistics =>
            {
                if (statistics.ContainsKey(key))
                {
                    statistics.TryGetTypedValue(key, out int item, Convert.ToInt32);
                    return item;
                }
                return 0;
            }).Sum();
        }

        private static int Min(IDictionary<string, object>[] results, string key)
        {
            return results.ToList().Select(statistics =>
            {
                if (statistics.ContainsKey(key))
                {
                    statistics.TryGetTypedValue(key, out int item, Convert.ToInt32);
                    return item;
                }
                return 0;
            }).Min();
        }

        public static void ResetCounters(StatisticsCollector statisticsCollecter)
        {
            statisticsCollecter.ResetGroupCounters();
            statisticsCollecter.ResetMessageCounters();
        }

        public static void ChangeFlagConnectionFlag(List<ConnectionState> connectionStates)
        {
            for (var i = 0; i < connectionStates.Count; i++)
            {
                if (connectionStates[i] == ConnectionState.Reconnect)
                {
                    connectionStates[i] = ConnectionState.Success;
                }
            }
        }
    }
}
