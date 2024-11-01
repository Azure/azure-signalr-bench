using System;
using Azure;
using Azure.SignalRBench.Coordinator.Entities;

namespace Portal.BasicAuth
{
    public class UserIdentity : IPerfTableEntity
    {
        public string Role { get; set; }
        public string Signature { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public DateTimeOffset? LastModified { get; set; }
    }
}