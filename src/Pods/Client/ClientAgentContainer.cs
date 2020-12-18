// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Messages;
using Microsoft.Extensions.Logging;

namespace Azure.SignalRBench.Client
{
    public class ClientAgentContainer
    {
        private readonly ClientAgentContext _context = new ClientAgentContext();
        private readonly MessageClientHolder _messageClientHolder;
        private readonly IClientAgent[] _clients;
        private readonly ILogger<ClientAgentContainer> _logger;
        private bool slowDown = false;

        public ClientAgentContainer(
            MessageClientHolder messageClientHolder,
            int startId,
            int localCount,
            SignalRProtocol protocol,
            bool isAnonymous,
            string url,
            ClientLifetimeDefinition lifetimeDefinition,
            Func<int, string[]> groupFunc,
            ILogger<ClientAgentContainer> logger)
        {
            _messageClientHolder = messageClientHolder;
            StartId = startId;
            LocalCount = localCount;
            //try to resolve service url
            try
            {
                var ips=Dns.GetHostAddresses(url);
                Console.WriteLine(($"ip count:{ips.Length}"));
                Console.WriteLine($"url is set to:{ips[0].ToString()}");
                Url ="http://"+ ips[0].ToString()+"/";
            }
            catch (Exception e)
            {
                Console.WriteLine($"resolve name {Url} failed");
                Console.WriteLine(e);
                throw;
            }
            Protocol = protocol;
            IsAnonymous = isAnonymous;
            LifetimeDefinition = lifetimeDefinition;
            GroupFunc = groupFunc;
            _logger = logger;
            _clients = new IClientAgent[localCount];
        }

        public int StartId { get; }

        public int LocalCount { get; }

        public string Url { get; }

        public SignalRProtocol Protocol { get; }

        public bool IsAnonymous { get; }

        public ClientLifetimeDefinition LifetimeDefinition { get; }

        public Func<int, string[]> GroupFunc { get; }

        public async Task StartAsync(double rate, CancellationToken cancellationToken)
        {
            for (int i = 0; i < _clients.Length; i++)
            {
                _clients[i] = new SignalRClientAgent(Url, Protocol, IsAnonymous ? null : $"user{StartId + i}", GroupFunc(i), _context);
            }

            using var cts = new CancellationTokenSource();
            using var linkSource = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);
                using var semaphore = GetRateControlSemaphore(rate, linkSource.Token);
                var count = 0;
                try
                {
                    await Task.WhenAll(
                        _clients.Select(async (c) =>
                        {
                            await semaphore.WaitAsync(cancellationToken);
                            while (!cancellationToken.IsCancellationRequested)
                            {
                                var stopWatch = new Stopwatch();
                                stopWatch.Start();
                                try
                                {
                                    await c.StartAsync(cancellationToken);
                                    Interlocked.Add(ref count, 1);
                                    stopWatch.Stop();
                                    _logger.LogInformation($"{Volatile.Read(ref count)} Connected. rate :{rate} Time cost:{stopWatch.ElapsedMilliseconds}");
                                    return;
                                }
                                catch (Exception ex)
                                {
                                    stopWatch.Stop();
                                    Volatile.Write(ref slowDown, true);
                                    _logger.LogError(ex,
                                        $"Failed to start {Volatile.Read(ref count)+1} client.,fail Time cost:{stopWatch.ElapsedMilliseconds}");
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
            _context.Reset();
            ScheduleReportedStatus(cancellationToken);
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
                    if (Volatile.Read(ref slowDown))
                    {
                        await Task.Delay(20, cancellationToken);
                        Volatile.Write(ref slowDown, false);
                    }

                    semaphore.Release(releaseCount);
                }
            }
        }

        public void ScheduleReportedStatus(CancellationToken cancellationToken)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(1000, cancellationToken);
                    _logger.LogInformation("reportClientStatus");
                    await _messageClientHolder.Client.ReportClientStatusAsync(_context.ClientStatus());
                }
            });
        }
    }
}