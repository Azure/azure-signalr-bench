using System.Collections.Generic;
using System.Linq;
using Azure.SignalRBench.AppServer.Readiness;
using Azure.SignalRBench.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.SignalR;

namespace Azure.SignalRBench.AppServer.Router
{
    public class ReplicaRouter : EndpointRouterDecorator
    {
        public override ServiceEndpoint GetNegotiateEndpoint(HttpContext context,
            IEnumerable<ServiceEndpoint> endpoints)
        {
            if (ReadinessProbe.TryGetEndpointOfNegotiation(context, out var probedEndpoint))
            {
                return ProbedEndpoint(endpoints, probedEndpoint);
            }

            return RandomReplicaEndpoint(endpoints).First();
        }

        public override IEnumerable<ServiceEndpoint> GetEndpointsForBroadcast(IEnumerable<ServiceEndpoint> endpoints)
        {
            return RandomReplicaEndpoint(endpoints);
        }

        public override IEnumerable<ServiceEndpoint> GetEndpointsForConnection(string connectionId,
            IEnumerable<ServiceEndpoint> endpoints)
        {
            return RandomReplicaEndpoint(endpoints);
        }

        public override IEnumerable<ServiceEndpoint> GetEndpointsForGroup(string groupName,
            IEnumerable<ServiceEndpoint> endpoints)
        {
            if (ReadinessProbe.TryGetEndpointOfGroup(groupName, out var probedEndpoint))
            {
                return new[] { ProbedEndpoint(endpoints, probedEndpoint) };
            }

            return RandomReplicaEndpoint(endpoints);
        }

        public override IEnumerable<ServiceEndpoint> GetEndpointsForUser(string userId,
            IEnumerable<ServiceEndpoint> endpoints)
        {
            return RandomReplicaEndpoint(endpoints);
        }

        // A readiness probe must reach exactly the endpoint it verifies; Single() fails the probe otherwise.
        private static ServiceEndpoint ProbedEndpoint(IEnumerable<ServiceEndpoint> endpoints, string probedEndpoint) =>
            endpoints.Single(endpoint => endpoint.Endpoint == probedEndpoint);

        private IEnumerable<ServiceEndpoint> RandomReplicaEndpoint(IEnumerable<ServiceEndpoint> endpoints)
        {
            var eps = endpoints.ToArray();
            var list = new List<ServiceEndpoint>();
            list.Add(eps[StaticRandom.Next(eps.Length)]);
            return list;
        }
    }
}