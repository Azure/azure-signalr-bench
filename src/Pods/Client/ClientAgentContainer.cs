// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure.SignalRBench.Client.ClientAgent;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Messages;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Azure.SignalRBench.Client
{
    public class ClientAgentContainer
    {
        private readonly ClientAgentContext _context;
        public MessageClientHolder MessageClientHolder { get; }
        private IClientAgent[] _clients = new IClientAgent[0];
        private readonly ILogger<ClientAgentContainer> _logger;

        private bool _slowDown = false;
        private int _startReport = 1;
        private readonly IClientAgentFactory _agentFactory;

        public ClientAgentContainer(
            MessageClientHolder messageClientHolder,
            Protocol protocol,
            bool isAnonymous,
            string url,
            ClientLifetimeDefinition lifetimeDefinition, IClientAgentFactory agentFactory,
            ILoggerFactory loggerFactory)
        {
            MessageClientHolder = messageClientHolder;
            _context = new ClientAgentContext(messageClientHolder.Client,loggerFactory.CreateLogger<ClientAgentContext>());
            _context.RetryPolicy=new RetryPolicy(_context);
            _logger = loggerFactory.CreateLogger<ClientAgentContainer>();
            //try to resolve service url
            //dirty logic, separate raw websocket
            if (url.Contains("Endpoint"))
            {
                Url = url;
                _logger.LogInformation($"RawWebsocket endpoint:{url}");
            }else if (url.Contains("http"))
            {
                Url = url.Trim() + "/";
            }
            else
            {
                var ips = Dns.GetHostAddresses(url);
                Url = "http://" + ips[0] + "/";
            }

            Protocol = protocol;
            IsAnonymous = isAnonymous;
            LifetimeDefinition = lifetimeDefinition;
            _agentFactory = agentFactory;
        }

        public int StartId { get; set; }

        public string Url { get; }

        public Protocol Protocol { get; }

        public bool IsAnonymous { get; }

        public ClientLifetimeDefinition LifetimeDefinition { get; }

        public Func<int, string[]> GroupFunc { get; set; }

        public int[] IndexMap { get; set; }

        public int ExpandConnections(int startId, int localCount, int[] indexMap, Func<int, string[]> groupFunc)
        {
            StartId = startId;
            var tmp = new IClientAgent[localCount];
            for (var i = 0; i < _clients.Length; i++)
            {
                tmp[i] = _clients[i];
            }

            var continueIndex = _clients.Length;
            _clients = tmp;
            GroupFunc = groupFunc;
            IndexMap = indexMap;
            return continueIndex;
        }

        public int GetGlobalIndex(int index) => IndexMap[index];

        public async Task StartAsync(int continueIndex, double rate, CancellationToken cancellationToken)
        {
            //Just in case socket in server hasn't opened
            await Task.Delay(1000);
            _context.Reset();
            for (var i = continueIndex; i < _clients.Length; i++)
            {
                var globalIndex = GetGlobalIndex(i);
                _clients[i] = _agentFactory.Create(Url, Protocol, GroupFunc(i),
                    globalIndex,
                    _context);
            }

            ScheduleReportedStatus(cancellationToken);
            using var cts = new CancellationTokenSource();
            using var linkSource = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);
            using var semaphore = GetRateControlSemaphore(rate, linkSource.Token);
            var index = continueIndex;
            var count = continueIndex;
            try
            {
                await Task.WhenAll(
                    _clients.Select(async (c, i) =>
                    {
                        if (i < continueIndex)
                            return;
                        var current = Interlocked.Increment(ref index);
                        await semaphore.WaitAsync(cancellationToken);
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            var stopWatch = new Stopwatch();
                            stopWatch.Start();
                            try
                            {
                                _logger.LogInformation($"{current} start to connect");
                                using var delayCts = new CancellationTokenSource();
                                using var lts = CancellationTokenSource.CreateLinkedTokenSource(delayCts.Token, cancellationToken);
                                lts.CancelAfter(5000);                              
                                await c.StartAsync(lts.Token);
                                stopWatch.Stop();
                                Interlocked.Add(ref count, 1);
                                _logger.LogInformation(
                                    $" Total {Volatile.Read(ref count)}, current {current} Connected. rate :{rate} Time cost:{stopWatch.ElapsedMilliseconds}");

                                return;
                            }
                            catch (Exception ex)
                            {
                                stopWatch.Stop();
                                Volatile.Write(ref _slowDown, true);
                                _logger.LogError(ex,
                                    $"Failed to start { current} client.,fail Time cost:{stopWatch.ElapsedMilliseconds}");
                                return;
                            }
                        }
                    }));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Start connections error");
            }
            finally
            {
                cts.Cancel();
            }
        }

        public async Task StopAsync(double rate)
        {
            using var cts = new CancellationTokenSource();
            //   using var semaphore = GetRateControlSemaphore(rate, cts.Token);
            try
            {
                _logger.LogInformation("Start stop connections");
                await Task.WhenAll(
                    _clients.Select(async c =>
                    {
                        //   await semaphore.WaitAsync();
                        await c.StopAsync();
                        //      _logger.LogInformation("Connection Stopped.");
                    }));
                _logger.LogInformation("All connections Stopped.");
            }
            finally
            {
                cts.Cancel();
            }
        }

        public void StartScenario(Func<int, Action<IClientAgent, CancellationToken>> func,
            CancellationToken cancellationToken)
        {
            for (int i = 0; i < _clients.Length; i++)
            {
                func(i)(_clients[i], cancellationToken);
            }
        }

        private SemaphoreSlim GetRateControlSemaphore(double rate, CancellationToken cancellationToken)
        {
            int maxCount = (int) Math.Ceiling(rate);
            var result = new SemaphoreSlim(0, maxCount);
            _ = ControlRate(result, rate, maxCount, cancellationToken);
            return result;
        }

        private async Task ControlRate(SemaphoreSlim semaphore, double rate, int maxCount,
            CancellationToken cancellationToken)
        {
            double current = 0;
            var stamp = Stopwatch.GetTimestamp();
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1, cancellationToken);
                var now = Stopwatch.GetTimestamp();
                ;
                current += (double) (now - stamp) / Stopwatch.Frequency * rate;
                var releaseCountRaw = Math.Floor(current);
                current -= releaseCountRaw;
                stamp = now;
                var releaseCount = Math.Min((int) releaseCountRaw, maxCount - semaphore.CurrentCount);
                if (releaseCount > 0)
                {
                    if (Volatile.Read(ref _slowDown))
                    {
                        await Task.Delay(20, cancellationToken);
                        Volatile.Write(ref _slowDown, false);
                    }

                    semaphore.Release(releaseCount);
                }
            }
        }

        public void ScheduleReportedStatus(CancellationToken cancellationToken)
        {
            if (Interlocked.CompareExchange(ref _startReport, 0, 1) == 1)
            {
                Task.Run(async () =>
                {
                    while (true)
                    {
                        try
                        {
                            await Task.Delay(1000, cancellationToken);
                            _logger.LogInformation("reportClientStatus");
                            await MessageClientHolder.Client.ReportClientStatusAsync(_context.ClientStatus());
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e,"report error");
                        }
                    }
                });
            }
        }
        
        public sealed class RetryPolicy : IRetryPolicy
        {
            private long _reconnecting;
            public RetryPolicy(ClientAgentContext context)
            {
                Task.Run(async () =>
                {
                    while (true)
                    {
                        await Task.Delay(1000);
                        _reconnecting = context.ReconnectingCount;
                    }
                });
            }
            public TimeSpan? NextRetryDelay(RetryContext retryContext)
            {
                return  TimeSpan.FromSeconds(1) + TimeSpan.FromSeconds(1+_reconnecting/50) * StaticRandom.NextDouble();
            }
               
        }
    }
}