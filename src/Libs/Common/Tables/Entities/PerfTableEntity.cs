using System;
using Azure.Data.Tables;

namespace Azure.SignalRBench.Coordinator.Entities
{
    public interface IPerfTableEntity : ITableEntity
    {
        public DateTimeOffset? LastModified { get; set; }
    }
}