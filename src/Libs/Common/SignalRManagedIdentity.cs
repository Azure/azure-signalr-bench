using System;
using System.Data.Common;
using System.Linq;
using Azure.Core;
using Azure.Identity;

namespace Azure.SignalRBench.Common
{
    public static class SignalRManagedIdentity
    {
        public static TokenCredential CreateCredential(string? managedIdentityClientId) =>
            new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = managedIdentityClientId
            });

        public static Uri[] ParseEndpoints(string? configuration)
        {
            if (string.IsNullOrWhiteSpace(configuration))
            {
                throw new ArgumentException("Azure SignalR endpoint configuration is required.", nameof(configuration));
            }

            return configuration
                .Split(new[] {' ', ','}, StringSplitOptions.RemoveEmptyEntries)
                .Select(ParseEndpoint)
                .ToArray();
        }

        private static Uri ParseEndpoint(string value)
        {
            if (Uri.TryCreate(value, UriKind.Absolute, out var endpoint))
            {
                return endpoint;
            }

            var connectionString = new DbConnectionStringBuilder {ConnectionString = value};
            if (connectionString.TryGetValue("Endpoint", out var endpointValue) &&
                Uri.TryCreate(Convert.ToString(endpointValue), UriKind.Absolute, out endpoint))
            {
                return endpoint;
            }

            throw new FormatException("Azure SignalR endpoint configuration must be an absolute URI or contain an Endpoint property.");
        }
    }
}