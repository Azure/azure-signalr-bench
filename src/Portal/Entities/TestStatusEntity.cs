using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Entities
{
    public class TestStatusEntity :TableEntity
    {
        public string Status { get; set; }
    }
}
