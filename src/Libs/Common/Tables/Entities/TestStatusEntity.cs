using System;
using Azure.SignalRBench.Common;

namespace Azure.SignalRBench.Coordinator.Entities
{
    public class TestStatusEntity : IPerfTableEntity
    {
        public string User { get; set; }

        public string Status { get; set; }

        public string Report { get; set; }

        public string Config { get; set; }

        public string? ErrorInfo { get; set; }

        public bool Healthy { get; set; }
        
        public string Dir { get; set; }
        
        public bool LongRun { get; set; }
        
        public string? QueueName { get; set; }
        
        public string? Location { get; set; }
        
        public string? TestId  => $"{PartitionKey}--{RowKey}";
        
        public string? LongRunContext { get; set; }
        
        public string? Check { get; set; }
        
        public string? GrafanaPath { get; set; }
        public string? K8sPath { get; set; }
        
        public string JobState { get; set; } = TestState.InProgress.ToString();
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public DateTimeOffset? LastModified { get; set; }
    }
}