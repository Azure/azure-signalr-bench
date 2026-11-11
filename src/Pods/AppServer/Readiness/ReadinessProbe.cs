// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.SignalR;

namespace Azure.SignalRBench.AppServer.Readiness
{
    /// <summary>
    /// Lets readiness probes target one specific Azure SignalR endpoint. <see cref="Router.ReplicaRouter"/> recognizes
    /// the probe markers and routes the probe to exactly the endpoint it verifies instead of a random one.
    /// </summary>
    internal static class ReadinessProbe
    {
        public const string NegotiateEndpointHeader = "X-Readiness-Probe-Endpoint";

        private const string GroupPrefix = "__readiness-probe__:";

        public static string GroupFor(ServiceEndpoint endpoint) => GroupPrefix + endpoint.Endpoint;

        public static bool TryGetEndpointOfGroup(string groupName, out string endpoint)
        {
            if (groupName.StartsWith(GroupPrefix, StringComparison.Ordinal))
            {
                endpoint = groupName.Substring(GroupPrefix.Length);
                return true;
            }

            endpoint = string.Empty;
            return false;
        }

        // Only honored for local requests, so remote clients cannot pick an endpoint.
        public static bool TryGetEndpointOfNegotiation(HttpContext context, out string endpoint)
        {
            endpoint = context.Request.Headers[NegotiateEndpointHeader].ToString();
            var remoteAddress = context.Connection.RemoteIpAddress;
            return endpoint.Length > 0 && remoteAddress != null && IPAddress.IsLoopback(remoteAddress);
        }
    }
}
