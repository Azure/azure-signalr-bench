using System.Collections.Generic;
using System.Linq;
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
            return RandomReplicaEndpoint(endpoints);
        }

        public override IEnumerable<ServiceEndpoint> GetEndpointsForUser(string userId,
            IEnumerable<ServiceEndpoint> endpoints)
        {
            return RandomReplicaEndpoint(endpoints);
        }

        private IEnumerable<ServiceEndpoint> RandomReplicaEndpoint(IEnumerable<ServiceEndpoint> endpoints)
        {
            var eps = endpoints.ToArray();
            var list = new List<ServiceEndpoint>();
            list.Add(eps[StaticRandom.Next(eps.Length)]);
            return list;
        }
    }
}