using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Coordinator.Entities;
using Azure.SignalRBench.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Portal.Controllers
{
    [Route("teststatus")]
    [ApiController]
    public class TestStatusContorller : ControllerBase
    {
        private readonly ILogger<TestStatusContorller> _logger;
        private readonly IPerfStorage _perfStorage;

        public TestStatusContorller(IPerfStorage perfStorage, ILogger<TestStatusContorller> logger)
        {
            _perfStorage = perfStorage;
            _logger = logger;
        }

        [HttpGet("list/{key?}")]
        public async Task<IEnumerable<TestStatusEntity>> Get(string key)
        {
            try
            {
                var table = await _perfStorage.GetTableAsync<TestStatusEntity>(PerfConstants.TableNames.TestStatus);
                TableQuery<TestStatusEntity> tableQuery=new TableQuery<TestStatusEntity>();
                tableQuery.OrderByDesc("Timestamp");
                if (string.IsNullOrEmpty(key))
                {
                    return await table.QueryAsync(tableQuery,30).ToListAsync();
                }
                var rows = await table.QueryAsync(
                    from row in table.Rows where row.PartitionKey == key select row).ToListAsync();
                rows.Sort((a, b) =>
                    b.Timestamp.CompareTo(a.Timestamp)
                );
                return rows;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Get test status error");
                throw;
            }
        }
        
        [HttpGet("dir/list/{dir?}")]
        public async Task<IEnumerable<TestStatusEntity>> DirList(string dir,string index)
        {
            try
            {
                var table = await _perfStorage.GetTableAsync<TestStatusEntity>(PerfConstants.TableNames.TestStatus);
                var rows = await table.QueryAsync(
                    from row in table.Rows where (row.Dir == dir) && (row.RowKey==index) select row).ToListAsync();
                rows.Sort((a, b) =>
                    b.Timestamp.CompareTo(a.Timestamp)
                );
                return rows;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Get test status error");
                throw;
            }
        }
    }
}