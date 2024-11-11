
using System;
using Azure;
using Azure.SignalRBench.Coordinator.Entities;

namespace Portal.Entity;

public class HostCluster: IPerfTableEntity
{
    public string Location => RowKey;
    public string GrafanaPath { get; set; }
    public string K8sKey => $"{RowKey}-k8s";
    public string K8SEndpoint { get; set; }
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public DateTimeOffset? LastModified { get; set; }
}