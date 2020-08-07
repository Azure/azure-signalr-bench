using Common;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Plugin.Microsoft.Azure.SignalR.Benchmark.Internals;
using Plugin.Microsoft.Azure.SignalR.Benchmark.Internals.AppServer;
using Plugin.Microsoft.Azure.SignalR.Benchmark.AgentMethods;
using Plugin.Microsoft.Azure.SignalR.Benchmark.AgentMethods.Statistics;
using Rpc.Service;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using static Plugin.Microsoft.Azure.SignalR.Benchmark.SignalREnums;
using AspNetHubConnection = Microsoft.AspNet.SignalR.Client.HubConnection;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    public static class SignalRUtils
    {
        public static void AddMethodAndType(IDictionary<string, object> data, IDictionary<string, object> parameters)
        {
            data[SignalRConstants.Method] = parameters[SignalRConstants.Method];
            data[SignalRConstants.Type] = parameters[SignalRConstants.Type];
        }

        public static void ShowConfiguration(IDictionary<string, object> dict)
        {
            Log.Information($"Handle step...{Environment.NewLine}Configuration: {Environment.NewLine}{dict.GetContents()}");
        }

        public static string GroupName(string type, int index) => $"{type}_{index}";

        public static string MessageLessThan(long latency) => $"{SignalRConstants.StatisticsLatencyLessThan}{latency}";

        public static string MessageGreaterOrEqualTo(long latency) => $"{SignalRConstants.StatisticsLatencyGreatEqThan}{latency}";

        public static Task MasterCreateConnection(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters, IList<IRpcClient> clients)
        {
            // Get parameters
            stepParameters.TryGetTypedValue(SignalRConstants.ConnectionTotal,
                out int connectionTotal, Convert.ToInt32);
            stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
            SignalRUtils.SaveTotalConnectionToContext(pluginParameters, type, connectionTotal);
            // Shuffle connection indexes
            var indexes = Enumerable.Range(0, connectionTotal).ToList();
            indexes.Shuffle();

            // Prepare configuration for each clients
            var packages = clients.Select((client, i) =>
            {
                (int beg, int end) = Util.GetConnectionRange(connectionTotal, i, clients.Count);
                var data = new Dictionary<string, object>(stepParameters);
                data[SignalRConstants.ConnectionIndex] = string.Join(',', indexes.GetRange(beg, end - beg));
                return new { Client = client, Data = data };
            });

            // Process on clients
            var results = from package in packages select package.Client.QueryAsync(package.Data);
            return Task.WhenAll(results);
        }

        public static async Task JoinToGroup(
            IHubConnectionAdapter connection,
            string groupName,
            StatisticsCollector statisticsCollector)
        {
            try
            {
                await connection.SendAsync(SignalRConstants.JoinGroupCallbackName, groupName);
            }
            catch
            {
                statisticsCollector.IncreaseJoinGroupFail();
            }
        }

        public static async Task LeaveFromGroup(
            IHubConnectionAdapter connection,
            string groupName,
            StatisticsCollector statisticsCollector)
        {
            try
            {
                await connection.SendAsync(SignalRConstants.LeaveGroupCallbackName, groupName);
            }
            catch
            {
                statisticsCollector.IncreaseLeaveGroupFail();
            }
        }
        #region save/fetch parameters to/from the context

        public static void SaveTotalConnectionToContext(
            IDictionary<string, object> pluginParameters,
            string type,
            int totalConnection)
        {
            pluginParameters[$"{SignalRConstants.ConnectionTotal}.{type}"] = totalConnection;
        }

        public static bool FetchTotalConnectionFromContext(
            IDictionary<string, object> pluginParameters,
            string type,
            out int totalConnections)
        {
            totalConnections = 0;
            if (pluginParameters.ContainsKey($"{SignalRConstants.ConnectionTotal}.{type}"))
            {
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionTotal}.{type}",
                                out int connectionCount, Convert.ToInt32);
                totalConnections = connectionCount;
                return true;
            }
            return false;
        }

        public static void SaveGroupInfoToContext(
            IDictionary<string, object> pluginParameters,
            string type,
            int groupCount,
            int totalConnection)
        {
            pluginParameters[$"{SignalRConstants.GroupCount}.{type}"] = groupCount;
            pluginParameters[$"{SignalRConstants.ConnectionTotal}.{type}"] = totalConnection;
        }

        public static void SaveConnectionStringToContext(
            IDictionary<string, object> pluginParameters,
            string type,
            string urls)
        {
            pluginParameters[$"{SignalRConstants.ConnectionString}.{type}"] = urls;
        }

        public static string FetchConnectionStringFromContext(
            IDictionary<string, object> pluginParameters,
            string type)
        {
            pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionString}.{type}",
                    out string connectionString, Convert.ToString);
            return connectionString;
        }

        public static void SaveConnectionInfoToContext(
            IDictionary<string, object> pluginParameters,
            string type,
            IList<IHubConnectionAdapter> connections,
            List<int> connectionIndex)
        {
            pluginParameters[$"{SignalRConstants.ConnectionStore}.{type}"] = connections;
            pluginParameters[$"{SignalRConstants.ConnectionIndex}.{type}"] = connectionIndex;
            pluginParameters[$"{SignalRConstants.RegisteredCallbacks}.{type}"] =
                new List<Action<IList<IHubConnectionAdapter>, StatisticsCollector>>();
        }
        #endregion

        public static IList<Action<IList<IHubConnectionAdapter>, StatisticsCollector>>
            FetchCallbacksFromContext(IDictionary<string, object> pluginParameters, string type)
        {
            pluginParameters.TryGetTypedValue($"{SignalRConstants.RegisteredCallbacks}.{type}",
                    out var registeredCallbacks, obj => (IList<Action<IList<IHubConnectionAdapter>, StatisticsCollector>>)obj);
            return registeredCallbacks;
        }

        public static void SaveConcurrentConnectionCountToContext(
            IDictionary<string, object> pluginParameters,
            string type,
            int concurrentConnection)
        {
            pluginParameters.TryAdd($"{SignalRConstants.ConcurrentConnection}.{type}", concurrentConnection);
        }

        public static int FetchConcurrentConnectionCountFromContext(
            IDictionary<string, object> pluginParameters,
            string type,
            int connectionCount)
        {
            var concurrentConnection = connectionCount > 100 ? 100 : connectionCount;
            if (pluginParameters.TryGetValue($"{SignalRConstants.ConcurrentConnection}.{type}", out _))
            {
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConcurrentConnection}.{type}",
                    out int value, Convert.ToInt32);
                concurrentConnection = value;
            }
            return concurrentConnection;
        }

        public static void CreateHttpClientManagerAndSaveToContext(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters)
        {
            stepParameters.TryGetTypedValue(SignalRConstants.Type,
                out string type, Convert.ToString);
            stepParameters.TryGetTypedValue(SignalRConstants.ConnectionIndex,
                out string connectionIndexString, Convert.ToString);
            var connectionIndex = connectionIndexString.Split(',').Select(ind => Convert.ToInt32(ind)).ToList();
            var connCount = connectionIndex.Count;
            var httpClientManager = new HttpClientManager(connCount/2);
            pluginParameters[$"{SignalRConstants.HttpClientManager}.{type}"] = httpClientManager;
        }

        public static HttpClientManager FetchHttpClientManagerFromContext(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters)
        {
            stepParameters.TryGetTypedValue(SignalRConstants.Type,
                out string type, Convert.ToString);
            if (pluginParameters.TryGetValue($"{SignalRConstants.HttpClientManager}.{type}", out _))
            {
                pluginParameters.TryGetTypedValue($"{SignalRConstants.HttpClientManager}.{type}",
                   out var httpClientManager, obj => (HttpClientManager)obj);
                return httpClientManager;
            }
            else
            {
                return null;
            }
        }

        public static void DiposeAllHttpClient(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters)
        {
            var httpClientManager = FetchHttpClientManagerFromContext(stepParameters, pluginParameters);
            if (httpClientManager != null)
            {
                httpClientManager.DisposeAllHttpMessageHandler();
            }
        }

        public static void MarkConnectionType(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters,
            ClientType clientType)
        {
            stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
            pluginParameters[$"{SignalRConstants.ConnectionType}.{type}"] = clientType.ToString();
        }

        public static ClientType GetClientTypeFromContext(
            IDictionary<string, object> pluginParameters,
            string type)
        {
            var ret = ClientType.AspNetCore;
            if (pluginParameters.TryGetValue($"{SignalRConstants.ConnectionType}.{type}", out _))
            {
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionType}.{type}",
                    out string ctype, Convert.ToString);
                if (Enum.TryParse(ctype, out SignalREnums.ClientType ct))
                {
                    ret = ct;
                }
            }
            return ret;
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

        public static bool isUsingInternalApp(IDictionary<string, object> stepParameters)
        {
            stepParameters.TryGetTypedValue(SignalRConstants.HubUrls,
                out string urls, Convert.ToString);
            return urls.StartsWith("Endpoint=");
        }

        public static async Task StartInternalAppServer(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters)
        {
            LocalhostAppServer localAppServer = null;
            stepParameters.TryGetTypedValue(SignalRConstants.Type,
                out string type, Convert.ToString);
            stepParameters.TryGetTypedValue(SignalRConstants.HubUrls,
                out string urls, Convert.ToString);
            if (pluginParameters.TryGetValue($"{SignalRConstants.LocalhostAppServer}.{type}", out _))
            {
                pluginParameters.TryGetTypedValue($"{SignalRConstants.LocalhostAppServer}.{type}",
                    out localAppServer, (obj) => (LocalhostAppServer)obj);
            }
            else
            {
                // connection string is stored in 'urls'
                localAppServer = new LocalhostAppServer(urls);
                pluginParameters[$"{SignalRConstants.LocalhostAppServer}.{type}"] = localAppServer;
            }
            if (!localAppServer.IsStarted)
            {
                await localAppServer.Start();
            }
        }

        public static async Task StopInternalAppServer(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters)
        {
            LocalhostAppServer localAppServer = null;
            stepParameters.TryGetTypedValue(SignalRConstants.Type,
                out string type, Convert.ToString);
            if (pluginParameters.TryGetValue($"{SignalRConstants.LocalhostAppServer}.{type}", out _))
            {
                pluginParameters.TryGetTypedValue($"{SignalRConstants.LocalhostAppServer}.{type}",
                    out localAppServer, (obj) => (LocalhostAppServer)obj);
            }
            if (localAppServer != null &&
                localAppServer.IsStarted)
            {
                await localAppServer.Stop();
            }
        }

        public static async Task StartNegotiationServer(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters)
        {
            NegotiationServer negotiationServer = null;
            stepParameters.TryGetTypedValue(SignalRConstants.Type,
                out string type, Convert.ToString);
            stepParameters.TryGetTypedValue(SignalRConstants.HubUrls,
                out string urls, Convert.ToString);
            if (pluginParameters.TryGetValue($"{SignalRConstants.NegotiationServer}.{type}", out _))
            {
                pluginParameters.TryGetTypedValue($"{SignalRConstants.NegotiationServer}.{type}",
                    out negotiationServer, (obj) => (NegotiationServer)obj);
            }
            else
            {
                // connection string is stored in 'urls'
                negotiationServer = new NegotiationServer(urls);
                pluginParameters[$"{SignalRConstants.NegotiationServer}.{type}"] = negotiationServer;
            }
            if (!negotiationServer.IsStarted)
            {
                await negotiationServer.Start();
            }
        }

        public static async Task StopNegotiationServer(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters)
        {
            NegotiationServer negotiationServer = null;
            stepParameters.TryGetTypedValue(SignalRConstants.Type,
                out string type, Convert.ToString);
            if (pluginParameters.TryGetValue($"{SignalRConstants.NegotiationServer}.{type}", out _))
            {
                pluginParameters.TryGetTypedValue($"{SignalRConstants.NegotiationServer}.{type}",
                    out negotiationServer, (obj) => (NegotiationServer)obj);
            }
            if (negotiationServer != null &&
                negotiationServer.IsStarted)
            {
                await negotiationServer.Stop();
            }
        }

        public static string GetNegotiationEndpoint(string hub, string userId)
        {
            return SignalRConstants.NegotiationUrl + "/" + hub + "?user=" + userId;
        }

        public static void AgentCreateConnection(
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
            var connections = CreateClientConnection(transportType, protocol, urls, connectionIndex, clientType);

            // Setup connection drop handler
            SetConnectionOnClose(connections);

            // Prepare plugin parameters
            SaveConnectionInfoToContext(pluginParameters, type, connections, connectionIndex);
            // record the client type for reconnect
            MarkConnectionType(stepParameters, pluginParameters, clientType);
            if (clientType == ClientType.DirectConnect)
            {
                // record the connection string for REST API send
                SaveConnectionStringToContext(pluginParameters, type, urls);
            }
        }

        public static bool HideMessageRoundTripLatency(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters)
        {
            stepParameters.TryGetTypedValue(SignalRConstants.Type,
                    out string type, Convert.ToString);
            pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionStore}.{type}",
                out IList<IHubConnectionAdapter> connections, (obj) => (IList<IHubConnectionAdapter>)obj);
            pluginParameters.TryGetTypedValue($"{SignalRConstants.StatisticsStore}.{type}",
                out StatisticsCollector statisticsCollector, obj => (StatisticsCollector)obj);
            pluginParameters.TryGetTypedValue($"{SignalRConstants.RegisteredCallbacks}.{type}",
                out var registeredCallbacks,
                obj => (IList<Action<IList<IHubConnectionAdapter>, StatisticsCollector>>)obj);
            pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionType}.{type}",
                out string connectionType, Convert.ToString);
            if (!Enum.TryParse<ClientType>(connectionType, out ClientType connType))
            {
                Log.Error($"Fail to parse {connectionType} to enum");
                return false;
            }
            if (!registeredCallbacks.Contains(RegisterCallbackBase.SetCallback))
            {
                RegisterCallbackBase.SetDummyLatencyCallback(connections, statisticsCollector);
                registeredCallbacks.Add(RegisterCallbackBase.SetDummyLatencyCallback);
                return true;
            }
            return false;
        }

        public static void FilterOnConnectedNotification(
            IDictionary<string, object> pluginParameters,
            string type)
        {
            pluginParameters.TryGetTypedValue($"{SignalRConstants.RegisteredCallbacks}.{type}",
                out var registeredCallbacks,
                obj => (IList<Action<IList<IHubConnectionAdapter>, StatisticsCollector>>)obj);
            pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionType}.{type}",
                out string connectionType, Convert.ToString);
            pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionStore}.{type}",
                out IList<IHubConnectionAdapter> connections, (obj) => (IList<IHubConnectionAdapter>)obj);
            pluginParameters.TryGetTypedValue($"{SignalRConstants.StatisticsStore}.{type}",
                            out StatisticsCollector statisticsCollector, (obj) => (StatisticsCollector)obj);
            if (!Enum.TryParse<ClientType>(connectionType, out ClientType connType))
            {
                Log.Error($"Fail to parse {connectionType} to enum");
                return;
            }
            if (connType == ClientType.AspNetCore)
            {
                if (!registeredCallbacks.Contains(RegisterCallbackBase.SetDummyCallbackOnConnected))
                {
                    RegisterCallbackBase.SetCallbackOnConnected(connections, statisticsCollector);
                    registeredCallbacks.Add(RegisterCallbackBase.SetDummyCallbackOnConnected);
                }
            }
        }

        public static void AddOnConnectedCallback(
            IList<IHubConnectionAdapter> connections,
            IList<Action<IList<IHubConnectionAdapter>, StatisticsCollector>> registeredCallbacks,
            StatisticsCollector statisticsCollector)
        {
            if (!registeredCallbacks.Contains(RegisterCallbackBase.SetDummyCallbackOnConnected))
            {
                RegisterCallbackBase.SetCallbackOnConnected(connections, statisticsCollector);
                registeredCallbacks.Add(RegisterCallbackBase.SetDummyCallbackOnConnected);
            }
        }

        public static async Task StartConnect((IHubConnectionAdapter Connection, int LocalIndex) package)
        {
            if (package.Connection == null ||
                package.Connection.GetStat() == ConnectionInternalStat.Active)
                return;

            try
            {
                using (var c = new CancellationTokenSource(TimeSpan.FromSeconds(3)))
                {
                    await package.Connection.StartAsync(c.Token);
                }
            }
            catch (Exception ex)
            {
                var message = $"Fail to start connection: {ex}";
                // only record error instead of throwing exception, allow reconnect
                Log.Error(message);
            }
        }

        public static async Task TakeActionAfterStartingConnect(
            (IHubConnectionAdapter Connection,
            int LocalIndex,
            SignalREnums.ActionAfterConnection Action,
            IDictionary<string, object> Context,
            string Type) package)
        {
            if (package.Connection == null ||
                package.Connection.GetStat() == ConnectionInternalStat.Active)
                return;

            try
            {
                using (var c = new CancellationTokenSource(TimeSpan.FromSeconds(3)))
                {
                    await package.Connection.StartAsync(c.Token);
                    // This does not support REST API client to join group if it drops.
                    if (package.Action == ActionAfterConnection.JoinToGroup)
                    {
                        if (package.Context.ContainsKey($"{SignalRConstants.GroupCount}.{package.Type}") &&
                            package.Context.ContainsKey($"{SignalRConstants.ConnectionTotal}.{package.Type}") &&
                            package.Context.ContainsKey($"{SignalRConstants.StatisticsStore}.{package.Type}"))
                        {
                            package.Context.TryGetTypedValue($"{SignalRConstants.GroupCount}.{package.Type}",
                                out int groupCount, Convert.ToInt32);
                            package.Context.TryGetTypedValue($"{SignalRConstants.ConnectionTotal}.{package.Type}",
                                out int connectionCount, Convert.ToInt32);
                            package.Context.TryGetTypedValue($"{SignalRConstants.StatisticsStore}.{package.Type}",
                                out StatisticsCollector statisticsCollector, (obj) => (StatisticsCollector)obj);
                            package.Context.TryGetTypedValue($"{SignalRConstants.ConnectionIndex}.{package.Type}",
                                out List<int> connectionIndex, (obj) => (List<int>)obj);
                            if (connectionCount >= groupCount)
                            {
                                var grp = GroupName(package.Type, connectionIndex[package.LocalIndex] % groupCount);
                                await JoinToGroup(package.Connection, grp, statisticsCollector);
                            }
                            else
                            {
                                for (var i = package.LocalIndex; i < groupCount; i += connectionCount)
                                {
                                    var grp = GroupName(package.Type, i);
                                    await JoinToGroup(package.Connection, grp, statisticsCollector);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
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
            var connections = from i in Enumerable.Range(0, connectionIndex.Count)
                              let userId = GenClientUserIdFromConnectionIndex(connectionIndex[i])
                              /* XXX: I don't know how to handle HttpClientHandler now.
                               * It is useful to connect self-signed https appserver,
                               * but it is disposed when reconnect which caused reconnect fail.
                               */
                              //let handler = new HttpClientHandler
                              //{
                              //    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                              //}
                              let negoEndPoint = GetNegotiationEndpoint(SignalRConstants.DefaultRestHubName, userId)
                              select new HubConnectionBuilder()
                              .ConfigureLogging(logger =>
                              {
                                  logger.ClearProviders();
                                  //logger.AddSerilog(dispose: true);
                                  //logger.SetMinimumLevel(LogLevel.Information);
                              })
                              .WithUrl(negoEndPoint, httpConnectionOptions =>
                              {
                                  //httpConnectionOptions.HttpMessageHandlerFactory = _ => handler;
                                  httpConnectionOptions.Transports = transportType;
                                  httpConnectionOptions.CloseTimeout = TimeSpan.FromMinutes(closeTimeout);
                              }) into builder
                              let hubConnection = protocolString.ToLower() == "messagepack" ?
                                                  builder.AddMessagePackProtocol().Build() :
                                                  builder.AddNewtonsoftJsonProtocol().Build()
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

        public static async Task<IServiceHubContext> CreateHubContextHelperAsync(
            ServiceTransportType serviceTransportType,
            IDictionary<string, object> pluginParameters,
            string type)
        {
            // The connection string is saved in context after finishing creating connection
            var connectionString = SignalRUtils.FetchConnectionStringFromContext(pluginParameters, type);
            var serviceManager = new ServiceManagerBuilder().WithOptions(option =>
            {
                option.ConnectionString = connectionString;
                option.ServiceTransportType = serviceTransportType;
            }).Build();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(logger =>
            {
                logger.ClearProviders();
                logger.AddSerilog(dispose: true);
                logger.SetMinimumLevel(LogLevel.Error);
            });

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var logFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var hubContext = await serviceManager.CreateHubContextAsync(SignalRConstants.DefaultRestHubName, logFactory);
            return hubContext;
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
            //var httpClientHandler = new HttpClientHandler
            //{
            //    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            //};
            var connections = from i in Enumerable.Range(0, connectionIndex.Count)
                              //let handler = httpClientHandler
                              select new HubConnectionBuilder()
                              .ConfigureLogging(logger =>
                              {
                                  logger.ClearProviders();
                                  logger.AddSerilog(dispose: true);
                                  logger.SetMinimumLevel(LogLevel.Error);
                              })
                              .WithUrl(urlList[connectionIndex[i] % urlList.Count()], httpConnectionOptions =>
                              {
                                  //httpConnectionOptions.HttpMessageHandlerFactory = h =>
                                  //{
                                  //    return urlList[connectionIndex[i] % urlList.Count()].StartsWith("https") ? handler : h;
                                  //};
                                  httpConnectionOptions.Transports = transportType;
                                  httpConnectionOptions.CloseTimeout = TimeSpan.FromMinutes(closeTimeout);
                              }) into builder
                              let hubConnection = protocolString.ToLower() == "messagepack" ?
                                                  builder.AddMessagePackProtocol().Build() :
                                                  builder.AddNewtonsoftJsonProtocol().Build()
                              select (IHubConnectionAdapter)(new SignalRCoreHubConnection(hubConnection));

            return connections.ToList();
        }

        public static void DumpConnectionInternalStat(IList<IHubConnectionAdapter> connections)
        {
            var success = 0;
            var failed = 0;
            var init = 0;
            for (var i = 0; i < connections.Count; i++)
            {
                switch (connections[i].GetStat())
                {
                    case ConnectionInternalStat.Active:
                        success++;
                        break;
                    case ConnectionInternalStat.Disposed:
                    case ConnectionInternalStat.Stopped:
                        failed++;
                        break;
                    case ConnectionInternalStat.Init:
                        init++;
                        break;
                }
            }
            Log.Information($"Connection status: success: {success}, failed: {failed}, init: {init}");
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
            IList<IHubConnectionAdapter> connections)
        {
            // Setup connection drop handler
            for (var i = 0; i < connections.Count(); i++)
            {
                connections[i].Closed += connections[i].OnClosed;
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
            return Convert.ToBase64String(message).Substring(0, len);
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
                sz += connId.ToString().Length;
            }
            if (payload.ContainsKey(SignalRConstants.GroupName))
            {
                payload.TryGetValue(SignalRConstants.GroupName, out var grpName);
                sz += grpName.ToString().Length;
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

        public static IDictionary<string, long> MergeConnectionStatistics(
            IDictionary<string, object>[] results, double[] percentileList)
        {
            var merged = new Dictionary<string, long>();
            var arr1 = MergeConnectionDistribution(results, SignalRConstants.StatisticsConnectionLifeSpan);
            var arr2 = MergeConnectionDistribution(results, SignalRConstants.StatisticsConnectionCost);
            var arr3 = MergeConnectionDistribution(results, SignalRConstants.StatisticsConnectionReconnectCost);
            var arr4 = MergeConnectionDistribution(results, SignalRConstants.StatisticsConnectionSLA);
            var arr5 = MergeConnectionDistribution(results, SignalRConstants.StatisticsConnectionOfflinetime);
            var dic1 = (from i in percentileList
                        select new { Key = $"{SignalRConstants.StatisticsConnectionLifeSpan}:{i}", Value = Percentile(arr1, i) })
                        .ToDictionary(entry => entry.Key, entry => entry.Value);
            var dic2 = (from i in percentileList
                        select new { Key = $"{SignalRConstants.StatisticsConnectionCost}:{i}", Value = Percentile(arr2, i) })
                        .ToDictionary(entry => entry.Key, entry => entry.Value);
            // remove connections who do not have reconnection cost
            var hasReconn = (from i in arr3
                             where i > 0
                             select i).ToArray();
            var dic3 = (from i in percentileList
                        select new { Key = $"{SignalRConstants.StatisticsConnectionReconnectCost}:{i}", Value = Percentile(hasReconn, i) })
                        .ToDictionary(entry => entry.Key, entry => entry.Value);
            var dic4 = (from i in percentileList
                        select new { Key = $"{SignalRConstants.StatisticsConnectionSLA}:{i}", Value = Percentile(arr4, i) })
                        .ToDictionary(entry => entry.Key, entry => entry.Value);
            var dic5 = (from i in percentileList
                        select new { Key = $"{SignalRConstants.StatisticsConnectionOfflinetime}:{i}", Value = Percentile(arr5, i) })
                        .ToDictionary(entry => entry.Key, entry => entry.Value);
            merged = merged.Union(dic1).Union(dic2).Union(dic3).Union(dic4).Union(dic5).ToDictionary(entry => entry.Key, entry => entry.Value);
            return merged;
        }

        public static long Percentile(int[] sequence, double excelPercentile)
        {
            var N = sequence.Length;
            if (N == 0)
            {
                return 0;
            }
            Array.Sort(sequence);
            double n = (N - 1) * excelPercentile + 1;
            // Another method: double n = (N + 1) * excelPercentile;
            if (n == 1d) return sequence[0];
            else if (n == N) return sequence[N - 1];
            else
            {
                int k = (int)n;
                int d = (int)(n - k);
                return sequence[k - 1] + d * (sequence[k] - sequence[k - 1]);
            }
        }

        private static int[] MergeConnectionDistribution(
            IDictionary<string, object>[] results, string key)
        {
            var arrays = results.ToList().Select(statistics =>
            {
                if (statistics.ContainsKey(key))
                {
                    statistics.TryGetTypedValue(key, out string item, Convert.ToString);
                    var values = item.Split(',').Select(ind => Convert.ToInt32(ind)).ToArray();
                    return values;
                }
                return null;
            });
            int finalLen = 0;
            foreach (var arr in arrays)
            {
                if (arr != null)
                {
                    finalLen += arr.Length;
                }
            }
            var result = new int[finalLen];
            int curPos = 0;
            foreach (var arr in arrays)
            {
                if (arr != null)
                {
                    Array.Copy(arr, 0, result, curPos, arr.Length);
                    curPos += arr.Length;
                }
            }
            Array.Sort(result);
            return result;
        }

        public static IDictionary<string, long> MergeStatistics(
            IDictionary<string, object>[] results,
            long latencyMax, long latencyStep)
        {
            var merged = new Dictionary<string, long>();

            // Sum of connection statistics
            merged[SignalRConstants.StatisticsConnectionConnectSuccess] =
                Sum(results, SignalRConstants.StatisticsConnectionConnectSuccess);
            merged[SignalRConstants.StatisticsConnectionConnectFail] =
                Sum(results, SignalRConstants.StatisticsConnectionConnectFail);
            merged[SignalRConstants.StatisticsConnectionReconnect] =
                Sum(results, SignalRConstants.StatisticsConnectionReconnect);

            // Sum of group statistics
            merged[SignalRConstants.StatisticsGroupJoinSuccess] =
                Sum(results, SignalRConstants.StatisticsGroupJoinSuccess);
            merged[SignalRConstants.StatisticsGroupJoinFail] =
                Sum(results, SignalRConstants.StatisticsGroupJoinFail);
            merged[SignalRConstants.StatisticsGroupLeaveSuccess] =
                Sum(results, SignalRConstants.StatisticsGroupLeaveSuccess);
            merged[SignalRConstants.StatisticsGroupLeaveFail] =
                Sum(results, SignalRConstants.StatisticsGroupLeaveFail);

            // Sum of "message:lt:latency"
            var SumMessageLatencyStatistics = (from i in Enumerable.Range(1, (int)latencyMax / (int)latencyStep)
                                               let latency = i * latencyStep
                                               select new { Key = MessageLessThan(latency), Sum = Sum(results, MessageLessThan(latency)) })
                                               .ToDictionary(entry => entry.Key, entry => entry.Sum);
            // Sum of "message:ge:latency"
            SumMessageLatencyStatistics[MessageGreaterOrEqualTo(latencyMax)] =
                Sum(results, MessageGreaterOrEqualTo(latencyMax));

            // Sum of total received message count
            merged[SignalRConstants.StatisticsMessageReceived] =
                SumMessageLatencyStatistics.Select(entry => entry.Value).Sum();

            // Sum of sent message statistics (should be calculated after "message:ge:latency")
            merged[SignalRConstants.StatisticsMessageSent] =
                Sum(results, SignalRConstants.StatisticsMessageSent);
            merged[SignalRConstants.StatisticsMessageSentSize] =
                Sum(results, SignalRConstants.StatisticsMessageSentSize);
            merged[SignalRConstants.StatisticsMessageReceivedSize] =
                Sum(results, SignalRConstants.StatisticsMessageReceivedSize);

            // Update epoch
            merged[SignalRConstants.StatisticsEpoch] = Min(results, SignalRConstants.StatisticsEpoch);
            merged[SignalRConstants.StatisticsSendingStep] = Min(results, SignalRConstants.StatisticsSendingStep);
            merged = merged.Union(SumMessageLatencyStatistics).ToDictionary(entry => entry.Key, entry => entry.Value);

            // Streaming item missing
            merged[SignalRConstants.StatisticsStreamingItemMissing] = Sum(results, SignalRConstants.StatisticsStreamingItemMissing);
            return merged;
        }

        public static async Task JoinGroupForConnection(
            int totalConnection,
            int groupCount,
            List<int> connectionIndex,
            Func<int, int, Task> JoinGroupOpt)
        {
            if (totalConnection > groupCount)
            {
                for (var i = 0; i < connectionIndex.Count; i++)
                {
                    await JoinGroupOpt(i, connectionIndex[i] % groupCount);
                }
            }
            else
            {
                var m = groupCount / totalConnection;
                for (var j = 0; j < m; j++)
                {
                    for (var i = 0; i < connectionIndex.Count; i++)
                    {
                        await JoinGroupOpt(i, j * totalConnection + connectionIndex[i]);
                    }
                }
                var n = groupCount % totalConnection;
                if (n > 0)
                {
                    for (var i = 0; i < connectionIndex.Count; i++)
                    {
                        if (connectionIndex[i] < n)
                        {
                            await JoinGroupOpt(i, m * totalConnection + connectionIndex[i]);
                        }
                    }
                }
            }
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

        public static long GetTimeoutPerConcurrentSpeed(int totalConnection, int concurrentConnections)
        {
            long expectedMilliseconds = (totalConnection / concurrentConnections) * 1000 * 2 * 1000000;
            if (expectedMilliseconds < SignalRConstants.MillisecondsToWait)
            {
                expectedMilliseconds = SignalRConstants.MillisecondsToWait;
            }
            return expectedMilliseconds;
        }
    }
}
