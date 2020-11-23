using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Coordinator.Entities;
using Azure.SignalRBench.Storage;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Portal.Controllers
{
    [Route("teststatus")]
    [ApiController]
    public class TestStatusContorller : ControllerBase
    {
        private IPerfStorage _perfStorage;

        public TestStatusContorller(IPerfStorage perfStorage)
        {
            _perfStorage = perfStorage;
        }

        [HttpGet("list/{key?}")]
        public async Task<IEnumerable<TestStatusEntity>> Get(string key)
        {
            try
            {
                var table = await _perfStorage.GetTableAsync<TestStatusEntity>(Constants.TableNames.TestStatus);
                var rows = await table.QueryAsync(string.IsNullOrEmpty(key)
                    ? table.Rows
                    : from row in table.Rows where row.PartitionKey == key select row).ToListAsync();
                rows.Sort((a, b) =>
                    b.Timestamp.CompareTo(a.Timestamp)
                );
                return rows;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}