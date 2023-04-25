using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Coordinator.Entities;
using Azure.SignalRBench.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Portal.Controllers
{
    [Route("teststatus")]
    [ApiController]
    public class TestStatusController : ControllerBase
    {
        private readonly ILogger<TestStatusController> _logger;
        private readonly IPerfStorage _perfStorage;

        public TestStatusController(IPerfStorage perfStorage, ILogger<TestStatusController> logger)
        {
            _perfStorage = perfStorage;
            _logger = logger;
        }

        [HttpGet("list/{key?}/{index?}")]
        public async Task<IEnumerable<TestStatusEntity>> Get(string key, string index)
        {
            try
            {
                var table = await _perfStorage.GetTableAsync<TestStatusEntity>(PerfConstants.TableNames.TestStatus);
                var onedayAgo = new DateTimeOffset(DateTime.UtcNow.AddDays(-1));
                if (string.IsNullOrEmpty(key))
                {
                    var result = await table
                        .QueryAsync(from row in table.Rows where row.Timestamp > onedayAgo select row).ToListAsync();
                    result.Sort((a, b) =>
                        b.Timestamp.CompareTo(a.Timestamp));
                    return result;
                }

                List<TestStatusEntity> rows = null;
                if (string.IsNullOrEmpty(index))
                    rows = await table.QueryAsync(
                        from row in table.Rows where row.PartitionKey == key select row).ToListAsync();
                else
                    rows = await table.QueryAsync(
                            from row in table.Rows where row.PartitionKey == key && row.RowKey == index select row)
                        .ToListAsync();
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
        public async Task<IEnumerable<TestStatusEntity>> DirList(string dir, string index)
        {
            try
            {
                index = index.ToLower();
                var table = await _perfStorage.GetTableAsync<TestStatusEntity>(PerfConstants.TableNames.TestStatus);
                var rows = await table.QueryAsync(
                    from row in table.Rows where (row.Dir == dir) && (row.RowKey == index) select row).ToListAsync();
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

        [Authorize(Policy = PerfConstants.Policy.RoleLogin,
            Roles = PerfConstants.Roles.Contributor + "," + PerfConstants.Roles.Pipeline)]
        [HttpGet("dir/check/{dir}")]
        public async Task<TestResult> DirCheck(string dir, string index)
        {
            try
            {
                index = index.ToLower();
                var table = await _perfStorage.GetTableAsync<TestStatusEntity>(PerfConstants.TableNames.TestStatus);
                var rows = await table.QueryAsync(
                    from row in table.Rows where (row.Dir == dir) && (row.RowKey == index) select row).ToListAsync();
                foreach (var row in rows)
                {
                    var state = Enum.Parse<TestState>(row.JobState);
                    if (state == TestState.InProgress)
                    {
                        return new TestResult()
                        {
                            Status = "wait"
                        };
                    }
                    else if (state == TestState.Failed)
                    {
                        return new TestResult()
                        {
                            Status = "fail"
                        };
                    }
                    else if (!(row.Healthy && string.IsNullOrWhiteSpace(row.Check) &&
                               string.IsNullOrWhiteSpace(row.ErrorInfo)))
                    {
                        return new TestResult()
                        {
                            Status = "fail"
                        };
                    }
                }

                return new TestResult()
                {
                    Status = "succeed"
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Get test status error");
                throw;
            }
        }
    }

    public class TestResult
    {
        public string Status { get; set; }
    }
}