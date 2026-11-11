// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Azure.SignalRBench.AppServer.Readiness
{
    /// <summary>
    /// Passes once clients of every configured Azure SignalR endpoint can negotiate with this server and be routed to it.
    /// </summary>
    /// <remarks>
    /// The SDK has no public readiness API (<see cref="ServiceEndpoint.Online"/> starts as true), so each endpoint is
    /// probed with public operations. With Microsoft Entra ID, server connections and client tokens authenticate
    /// differently (client tokens need the endpoint's access key), so both are probed.
    /// </remarks>
    public sealed class AzureSignalRReadinessCheck : IReadinessCheck
    {
        private const string ProbeConnectionId = "readiness-probe";
        private static readonly TimeSpan RetryInterval = TimeSpan.FromMilliseconds(500);

        private readonly IHubContext<Hub.SignalRBench> _hubContext;
        private readonly IServer _server;
        private readonly ServiceEndpoint[] _endpoints;
        private readonly ILogger<AzureSignalRReadinessCheck> _logger;

        public AzureSignalRReadinessCheck(IHubContext<Hub.SignalRBench> hubContext, IServer server,
            IOptions<ServiceOptions> options, ILogger<AzureSignalRReadinessCheck> logger)
        {
            _hubContext = hubContext;
            _server = server;
            _endpoints = options.Value.Endpoints;
            _logger = logger;
        }

        public string Name => "Azure SignalR endpoints";

        public async Task WaitUntilReadyAsync(CancellationToken cancellationToken)
        {
            using var localServer = new HttpClient { BaseAddress = LocalAddress() };
            await Task.WhenAll(_endpoints.Select(endpoint =>
                WaitForEndpointAsync(endpoint, localServer, cancellationToken)));
        }

        private async Task WaitForEndpointAsync(ServiceEndpoint endpoint, HttpClient localServer,
            CancellationToken cancellationToken)
        {
            await RetryUntilSuccessAsync(endpoint, "server connections",
                () => ProbeServerConnectionsAsync(endpoint, cancellationToken), cancellationToken);
            await RetryUntilSuccessAsync(endpoint, "client negotiation",
                () => ProbeNegotiationAsync(endpoint, localServer, cancellationToken), cancellationToken);
            _logger.LogInformation("Azure SignalR endpoint {Endpoint} is ready.", endpoint.Endpoint);
        }

        // Completes only after the service acknowledges it over one of this server's connections to the endpoint.
        // The probed connection does not exist, so nothing is changed.
        private Task ProbeServerConnectionsAsync(ServiceEndpoint endpoint, CancellationToken cancellationToken) =>
            _hubContext.Groups.RemoveFromGroupAsync(ProbeConnectionId, ReadinessProbe.GroupFor(endpoint),
                cancellationToken);

        // Negotiates through this server exactly like a client does, which issues a client token for the endpoint.
        private static async Task ProbeNegotiationAsync(ServiceEndpoint endpoint, HttpClient localServer,
            CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post,
                $"{Startup.HUB_NAME}/negotiate?negotiateVersion=1");
            request.Headers.Add(ReadinessProbe.NegotiateEndpointHeader, endpoint.Endpoint);
            using var response = await localServer.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        private async Task RetryUntilSuccessAsync(ServiceEndpoint endpoint, string probe, Func<Task> attempt,
            CancellationToken cancellationToken)
        {
            Exception? lastError = null;
            while (true)
            {
                try
                {
                    await attempt();
                    return;
                }
                catch (Exception e) when (!cancellationToken.IsCancellationRequested)
                {
                    _logger.Log(lastError == null ? LogLevel.Information : LogLevel.Debug,
                        "Azure SignalR endpoint {Endpoint}: {Probe} not ready yet: {Reason}",
                        endpoint.Endpoint, probe, e.Message);
                    lastError = e;
                }

                try
                {
                    await Task.Delay(RetryInterval, cancellationToken);
                }
                catch (OperationCanceledException e)
                {
                    throw new OperationCanceledException(
                        $"Azure SignalR endpoint {endpoint.Endpoint}: {probe} not ready. Last error: {lastError?.Message}",
                        e, cancellationToken);
                }
            }
        }

        private Uri LocalAddress()
        {
            var address = _server.Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault()
                          ?? throw new InvalidOperationException("The web server has no listening address.");
            // Listening addresses may use wildcard hosts, e.g. http://*:8080 or http://[::]:8080.
            var port = new Uri(address.Replace("*", "localhost").Replace("+", "localhost")).Port;
            return new Uri($"http://127.0.0.1:{port}/");
        }
    }
}
