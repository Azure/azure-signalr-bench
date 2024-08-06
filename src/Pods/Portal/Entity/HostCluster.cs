using Microsoft.Azure.Cosmos.Table;

namespace Portal.Entity;

public class HostCluster: TableEntity
{
    public string Location => RowKey;
    public string GrafanaPath { get; set; }
    public string K8sKey => $"{RowKey}-k8s";
    public string K8SEndpoint { get; set; }
}