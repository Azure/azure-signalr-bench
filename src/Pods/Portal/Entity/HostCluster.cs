using Microsoft.Azure.Cosmos.Table;

namespace Portal.Entity;

public class HostCluster: TableEntity
{
    public string Location => RowKey;
}