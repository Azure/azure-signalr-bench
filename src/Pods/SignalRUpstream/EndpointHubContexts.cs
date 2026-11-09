using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Microsoft.Azure.SignalR;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SignalRUpstream
{
    /// <summary>
    /// Owns one long-lived <see cref="ServiceHubContext"/> per configured SignalR endpoint (primary and replicas).
    /// Sharing them across requests lets the SDK reuse one cached Microsoft Entra access key per endpoint instead of
    /// acquiring a new key for every negotiate or REST call.
    /// </summary>
    /// <remarks>
    /// Registered as a hosted service before <c>ServerHostedService</c>, so the server only reports Ready after every
    /// endpoint has been initialized and has successfully issued a client token.
    /// </remarks>
    public sealed class EndpointHubContexts : IHostedService, IAsyncDisposable
    {
        private const string WarmUpUserId = "upstream-warmup";

        private readonly ServiceEndpoint[] _endpoints;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<EndpointHubContexts> _logger;
        private EndpointHubContext[] _hubContexts = Array.Empty<EndpointHubContext>();

        public EndpointHubContexts(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            HubName = NameConverter.GenerateHubName(configuration[PerfConstants.ConfigurationKeys.TestIdKey]);
            var credential = SignalRManagedIdentity.CreateCredential(
                configuration[PerfConstants.ConfigurationKeys.MsiAppId]);
            // Endpoints are created once, outside the SDK options callbacks: the SDK re-runs those callbacks for its
            // internal providers, and each new ServiceEndpoint would bring its own access key cache.
            _endpoints = SignalRManagedIdentity.ParseEndpoints(
                    configuration[PerfConstants.ConfigurationKeys.ConnectionString])
                .Select(endpoint => new ServiceEndpoint(endpoint, credential))
                .ToArray();
            if (_endpoints.Length == 0)
            {
                throw new InvalidOperationException("At least one SignalR endpoint must be configured.");
            }
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<EndpointHubContexts>();
        }

        public string HubName { get; }

        /// <summary>
        /// Picks a random endpoint so that clients and REST calls are spread across the primary and its replicas.
        /// </summary>
        public ServiceHubContext PickRandom()
        {
            var hubContexts = Volatile.Read(ref _hubContexts);
            if (hubContexts.Length == 0)
            {
                throw new InvalidOperationException("SignalR hub contexts are not initialized.");
            }

            return hubContexts[StaticRandom.Next(hubContexts.Length)].HubContext;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var created = new List<EndpointHubContext>(_endpoints.Length);
            try
            {
                foreach (var endpoint in _endpoints)
                {
                    created.Add(await CreateAsync(endpoint, cancellationToken));
                }

                // Authorize every endpoint before Ready. The SDK does not deduplicate concurrent first-time access key
                // acquisitions, and a failed acquisition blocks token generation for minutes, so it must not happen
                // in the burst of client negotiations that follows Ready.
                await Task.WhenAll(created.Select(hubContext => WarmUpAsync(hubContext, cancellationToken)));
            }
            catch
            {
                await DisposeAllAsync(created);
                throw;
            }

            Volatile.Write(ref _hubContexts, created.ToArray());
            _logger.LogInformation("Hub {Hub} is ready on {Count} SignalR endpoints.", HubName, created.Count);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public ValueTask DisposeAsync() =>
            DisposeAllAsync(Interlocked.Exchange(ref _hubContexts, Array.Empty<EndpointHubContext>()));

        private async Task<EndpointHubContext> CreateAsync(ServiceEndpoint endpoint, CancellationToken cancellationToken)
        {
            var serviceEndpoints = new[] { endpoint };
            var serviceManager = new ServiceManagerBuilder()
                .WithOptions(options =>
                {
                    options.ServiceEndpoints = serviceEndpoints;
                    // Transient (REST) mode supports only one endpoint per service manager.
                    options.ServiceTransportType = ServiceTransportType.Transient;
                })
                .WithLoggerFactory(_loggerFactory)
                .BuildServiceManager();
            try
            {
                var hubContext = await serviceManager.CreateHubContextAsync(HubName, cancellationToken);
                return new EndpointHubContext(endpoint, serviceManager, hubContext);
            }
            catch
            {
                serviceManager.Dispose();
                throw;
            }
        }

        private static async Task WarmUpAsync(EndpointHubContext hubContext, CancellationToken cancellationToken)
        {
            try
            {
                await hubContext.HubContext.NegotiateAsync(
                    new NegotiationOptions { UserId = WarmUpUserId }, cancellationToken);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    $"Cannot issue client tokens for SignalR endpoint {hubContext.Endpoint.Endpoint}. " +
                    "Check the credential, its role assignment on the resource and connectivity to the endpoint.", e);
            }
        }

        // Best effort: a failing context must not prevent the remaining resources from being released, nor mask the
        // exception that triggered the cleanup.
        private async ValueTask DisposeAllAsync(IEnumerable<EndpointHubContext> hubContexts)
        {
            foreach (var hubContext in hubContexts)
            {
                try
                {
                    await hubContext.HubContext.DisposeAsync();
                }
                catch (Exception e)
                {
                    LogDisposeFailure(e, hubContext);
                }

                try
                {
                    hubContext.ServiceManager.Dispose();
                }
                catch (Exception e)
                {
                    LogDisposeFailure(e, hubContext);
                }
            }
        }

        private void LogDisposeFailure(Exception exception, EndpointHubContext hubContext) =>
            _logger.LogWarning(exception, "Failed to dispose SignalR clients for endpoint {Endpoint}.",
                hubContext.Endpoint.Endpoint);

        private sealed record EndpointHubContext(
            ServiceEndpoint Endpoint, ServiceManager ServiceManager, ServiceHubContext HubContext);
    }
}
