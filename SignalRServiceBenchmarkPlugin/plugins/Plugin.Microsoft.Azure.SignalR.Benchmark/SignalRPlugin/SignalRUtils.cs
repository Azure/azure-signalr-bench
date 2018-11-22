using Common;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Plugin.Base;
using Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods.Statistics;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    public static class SignalRUtils
    {
        public static string GroupName(string type, int index) => $"{type}:{index}";

        public static string MessageLessThan(long latency) => $"message:lt:{latency}";

        public static string MessageGreaterOrEqaulTo(long latency) => $"message:ge:{latency}";

        public static async Task StartConnect((HubConnection Connection, int LocalIndex, List<SignalREnums.ConnectionState> connectionsSuccessFlag, 
            SignalREnums.ConnectionState NormalState, SignalREnums.ConnectionState AbnormalState) package)
        {
            if (package.Connection == null || package.connectionsSuccessFlag[package.LocalIndex] != SignalREnums.ConnectionState.Init) return;
            
            try
            {
                await package.Connection.StartAsync();
                package.connectionsSuccessFlag[package.LocalIndex] = package.NormalState;
            }
            catch (Exception ex)
            {
                package.connectionsSuccessFlag[package.LocalIndex] = package.AbnormalState;
                var message = $"Fail to start connection: {ex}";
                Log.Error(message);
                throw;
            }
        }

        public static IList<HubConnection> CreateConnections(IList<int> connectionIndex, string urls, string transportTypeString, string protocolString, int closeTimeout)
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
                              select protocolString.ToLower() == "messagepack" ? builder.AddMessagePackProtocol().Build() : builder.Build();

            return connections.ToList();
        }

        public static void SetConnectionOnClose(IList<HubConnection> connections, IList<SignalREnums.ConnectionState> connectionsSuccessFlag)
        {
            // Setup connection drop handler
            for (var i = 0; i < connections.Count(); i++)
            {
                var index = i;
                connections[i].Closed += e =>
                {
                    connectionsSuccessFlag[index] = SignalREnums.ConnectionState.Fail;
                    return Task.CompletedTask;
                };
            }
        }

        public static IDictionary<string, int> MergeStatistics(IDictionary<string, object>[] results, string type, long latencyMax, long latencyStep)
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
                                               select new { Key = SignalRUtils.MessageLessThan(latency), Sum = Sum(results, SignalRUtils.MessageLessThan(latency)) }).ToDictionary(entry => entry.Key, entry => entry.Sum);
            // Sum of "message:ge:latency"
            SumMessageLatencyStatistics[SignalRUtils.MessageGreaterOrEqaulTo(latencyMax)] = Sum(results, SignalRUtils.MessageGreaterOrEqaulTo(latencyMax));

            // Sum of total received message count
            merged[SignalRConstants.StatisticsMessageReceived] = SumMessageLatencyStatistics.Select(entry => entry.Value).Sum();

            // Sum of sent message statistics (should be calculated after "message:ge:latency")
            merged[SignalRConstants.StatisticsMessageSent] = Sum(results, SignalRConstants.StatisticsMessageSent);

            // Update epoch
            merged[SignalRConstants.StatisticsEpoch] = Min(results, SignalRConstants.StatisticsEpoch);

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

        public static void ChangeFlagConnectionFlag(List<SignalREnums.ConnectionState> connectionStates)
        {
            for (var i = 0; i < connectionStates.Count; i++)
            {
                if (connectionStates[i] == SignalREnums.ConnectionState.Reconnect)
                {
                    connectionStates[i] = SignalREnums.ConnectionState.Success;
                }
            }
        }
    }
}
