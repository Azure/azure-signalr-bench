// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.SignalRBench.Common;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.SignalRBench.Client
{
    public class ClientAgentContainer
    {
        private readonly ClientAgentContext _context = new ClientAgentContext();
        private readonly ClientAgent[] _clients;
        private readonly SetClientRangeParameters _clientRange;

        public ClientAgentContainer(SetClientRangeParameters clientRange, SignalRProtocol protocol, bool isAnonymous, string url)
        {
            _clients = new ClientAgent[clientRange.Count];
            _clientRange = clientRange;
            Url = url;
            Protocol = protocol;
            IsAnonymous = isAnonymous;
        }

        public string Url { get; }

        public SignalRProtocol Protocol { get; }

        public bool IsAnonymous { get; }

        public async Task StartAsync(double rate, SetScenarioParameters scenario, CancellationToken cancellationToken)
        {
            for (int i = 0; i < _clients.Length; i++)
            {
                _clients[i] = new ClientAgent(Url, Protocol, GetGroups(scenario, _clientRange.StartId + i), IsAnonymous ? null : $"user{_clientRange.StartId + i}", _context);
            }
            using var cts = new CancellationTokenSource();
            using var linkSource = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);
            using var semaphore = GetRateControlSemaphore(rate, linkSource.Token);
            try
            {
                await Task.WhenAll(
                    _clients.Select(async c =>
                    {
                        await semaphore.WaitAsync(cancellationToken);
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            try
                            {
                                await c.StartAsync(cancellationToken);
                                return;
                            }
                            catch (Exception)
                            {
                                // todo : log.
                            }
                        }
                    }));
            }
            finally
            {
                cts.Cancel();
            }
        }

        public async Task StopAsync(double rate)
        {
            using var cts = new CancellationTokenSource();
            using var semaphore = GetRateControlSemaphore(rate, cts.Token);
            try
            {
                await Task.WhenAll(
                    _clients.Select(async c =>
                    {
                        await semaphore.WaitAsync();
                        await c.StopAsync();
                    }));
            }
            finally
            {
                cts.Cancel();
            }
        }

        private SemaphoreSlim GetRateControlSemaphore(double rate, CancellationToken cancellationToken)
        {
            int maxCount = (int)Math.Ceiling(rate);
            var result = new SemaphoreSlim(0, maxCount);
            _ = ControlRate(result, rate, maxCount, cancellationToken);
            return result;
        }

        private async Task ControlRate(SemaphoreSlim semaphore, double rate, int maxCount, CancellationToken cancellationToken)
        {
            double current = 0;
            var stamp = Stopwatch.GetTimestamp();
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1, cancellationToken);
                var now = Stopwatch.GetTimestamp(); ;
                current += (double)(now - stamp) / Stopwatch.Frequency * rate;
                var releaseCountRaw = Math.Floor(current);
                current -= releaseCountRaw;
                stamp = now;
                var releaseCount = Math.Min((int)releaseCountRaw, maxCount - semaphore.CurrentCount);
                if (releaseCount > 0)
                {
                    semaphore.Release(releaseCount);
                }
            }
        }

        private static string[] GetGroups(SetScenarioParameters scenario, int clientIndex)
        {
            // todo : get groups.
            return Array.Empty<string>();
        }
    }
}
